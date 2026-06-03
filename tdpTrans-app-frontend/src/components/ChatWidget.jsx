import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { fetchChatContacts, fetchChatHistory, sendChatMessage } from '../api/chatApi';
import { getChatWsUrl } from '../api/config';
import { getChatClientKey, getStoredUser, isAdminUser } from '../utils/session';
import styles from './ChatWidget.module.css';

const hiddenPaths = new Set(['/login', '/signup', '/recover-password', '/']);

const statusLabels = {
  connecting: 'Conectare',
  connected: 'Conectat',
  reconnecting: 'Reconectare',
  offline: 'Offline',
};

const formatTime = (timestamp) =>
  new Intl.DateTimeFormat('ro-RO', {
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(timestamp));

const sortMessages = (messages) =>
  [...messages].sort(
    (leftMessage, rightMessage) =>
      new Date(leftMessage.timestampUtc).getTime() - new Date(rightMessage.timestampUtc).getTime()
  );

const mergeMessages = (currentMessages, incomingMessages) => {
  const uniqueMessages = new Map(currentMessages.map((message) => [message.id, message]));

  incomingMessages.forEach((message) => {
    uniqueMessages.set(message.id, message);
  });

  return sortMessages(Array.from(uniqueMessages.values()));
};

const getConversationPartnerId = (message, currentUserId) =>
  message.senderUserId === currentUserId ? message.recipientUserId : message.senderUserId;

const ChatWidget = () => {
  const location = useLocation();
  const user = useMemo(() => getStoredUser(), [location.pathname]);
  const adminUser = isAdminUser(user);
  const userId = user?.id ?? null;
  const userRoleName = user?.roleName ?? '';
  const userFullName = user?.fullName ?? '';
  const [contacts, setContacts] = useState([]);
  const [selectedContactId, setSelectedContactId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [draft, setDraft] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const [status, setStatus] = useState('connecting');
  const [unreadByContact, setUnreadByContact] = useState({});
  const [error, setError] = useState('');
  const [contactsError, setContactsError] = useState('');
  const [isLoadingContacts, setIsLoadingContacts] = useState(false);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const socketRef = useRef(null);
  const isOpenRef = useRef(false);
  const messagesEndRef = useRef(null);
  const hasConnectedRef = useRef(false);
  const messageIdsRef = useRef(new Set());
  const selectedContactIdRef = useRef(null);

  const unreadCount = useMemo(
    () => Object.values(unreadByContact).reduce((total, count) => total + count, 0),
    [unreadByContact]
  );

  const selectedContact = useMemo(
    () => contacts.find((contact) => contact.id === selectedContactId) ?? null,
    [contacts, selectedContactId]
  );

  useEffect(() => {
    isOpenRef.current = isOpen;
  }, [isOpen]);

  useEffect(() => {
    selectedContactIdRef.current = selectedContactId;
  }, [selectedContactId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isOpen]);

  useEffect(() => {
    if (!user || hiddenPaths.has(location.pathname)) {
      return undefined;
    }

    const clientKey = getChatClientKey();
    let disposed = false;
    let reconnectTimerId = null;
    let shouldReconnect = true;

    setContacts([]);
    setSelectedContactId(null);
    setMessages([]);
    setDraft('');
    setError('');
    setContactsError('');
    setUnreadByContact({});
    setStatus('connecting');
    setIsLoadingContacts(true);
    hasConnectedRef.current = false;
    messageIdsRef.current = new Set();

    const loadContacts = async () => {
      try {
        const nextContacts = await fetchChatContacts();
        if (!disposed) {
          setContacts(nextContacts);
          setContactsError('');
          setUnreadByContact((currentValue) => {
            const allowedContactIds = new Set(nextContacts.map((contact) => contact.id));
            return Object.fromEntries(
              Object.entries(currentValue).filter(([contactId]) => allowedContactIds.has(Number(contactId)))
            );
          });
          setSelectedContactId((currentValue) => {
            if (currentValue && nextContacts.some((contact) => contact.id === currentValue)) {
              return currentValue;
            }

            return nextContacts[0]?.id ?? null;
          });
        }
      } catch (contactsLoadError) {
        if (!disposed) {
          setContactsError(contactsLoadError.message);
        }
      } finally {
        if (!disposed) {
          setIsLoadingContacts(false);
        }
      }
    };

    const connect = () => {
      setStatus(hasConnectedRef.current ? 'reconnecting' : 'connecting');
      const socket = new WebSocket(
        `${getChatWsUrl()}?accessToken=${encodeURIComponent(user.accessToken)}&clientKey=${encodeURIComponent(clientKey)}`
      );
      socketRef.current = socket;

      socket.onopen = () => {
        if (!disposed) {
          hasConnectedRef.current = true;
          setStatus('connected');
          setError('');
        }
      };

      socket.onmessage = (event) => {
        try {
          const message = JSON.parse(event.data);
          const partnerUserId = getConversationPartnerId(message, user.id);
          const isSelectedConversation = selectedContactIdRef.current === partnerUserId;
          const partnerContact =
            message.senderUserId === user.id
              ? {
                  id: message.recipientUserId,
                  fullName: message.recipientName,
                  roleName: message.recipientRole,
                  email: '',
                }
              : {
                  id: message.senderUserId,
                  fullName: message.senderName,
                  roleName: message.senderRole,
                  email: '',
                };

          setContacts((currentContacts) => {
            if (currentContacts.some((contact) => contact.id === partnerUserId)) {
              return currentContacts;
            }

            return [...currentContacts, partnerContact].sort((leftContact, rightContact) =>
              leftContact.fullName.localeCompare(rightContact.fullName, 'ro')
            );
          });

          if (isSelectedConversation) {
            if (messageIdsRef.current.has(message.id)) {
              return;
            }

            messageIdsRef.current.add(message.id);
            setMessages((currentMessages) => mergeMessages(currentMessages, [message]));
          }

          if (message.senderUserId !== user.id && (!isOpenRef.current || !isSelectedConversation)) {
            setUnreadByContact((currentValue) => ({
              ...currentValue,
              [partnerUserId]: (currentValue[partnerUserId] ?? 0) + 1,
            }));

            if (!selectedContactIdRef.current) {
              setSelectedContactId(partnerUserId);
            }
          }
        } catch {
          setError('A aparut o problema la afisarea mesajelor.');
        }
      };

      socket.onclose = () => {
        socketRef.current = null;

        if (!disposed) {
          setStatus('offline');

          if (!hasConnectedRef.current) {
            setError('Chat-ul nu a putut fi conectat pentru sesiunea curenta.');
          }
        }

        if (!disposed && shouldReconnect) {
          reconnectTimerId = window.setTimeout(connect, 2000);
        }
      };

      socket.onerror = () => {
        if (!disposed) {
          setStatus('offline');
        }
      };
    };

    loadContacts();
    connect();

    return () => {
      disposed = true;
      shouldReconnect = false;
      if (reconnectTimerId) {
        window.clearTimeout(reconnectTimerId);
      }
      if (socketRef.current?.readyState === WebSocket.OPEN) {
        socketRef.current.close();
      }
      socketRef.current = null;
    };
  }, [location.pathname, userId, userRoleName]);

  useEffect(() => {
    if (!user || hiddenPaths.has(location.pathname)) {
      return undefined;
    }

    if (!selectedContactId) {
      setMessages([]);
      setError('');
      messageIdsRef.current = new Set();
      return undefined;
    }

    let disposed = false;
    setMessages([]);
    setDraft('');
    setError('');
    setIsLoadingHistory(true);
    messageIdsRef.current = new Set();
    setUnreadByContact((currentValue) => {
      const nextValue = { ...currentValue };
      delete nextValue[selectedContactId];
      return nextValue;
    });

    const loadHistory = async () => {
      try {
        const history = await fetchChatHistory(selectedContactId);
        if (!disposed) {
          setMessages(history);
          messageIdsRef.current = new Set(history.map((message) => message.id));
        }
      } catch (historyError) {
        if (!disposed) {
          setError(historyError.message);
        }
      } finally {
        if (!disposed) {
          setIsLoadingHistory(false);
        }
      }
    };

    loadHistory();

    return () => {
      disposed = true;
    };
  }, [location.pathname, selectedContactId, userId]);

  if (!user || hiddenPaths.has(location.pathname)) {
    return null;
  }

  const handleToggle = () => {
    setIsOpen((currentValue) => !currentValue);
  };

  const handleSend = async () => {
    const trimmedDraft = draft.trim();

    if (!trimmedDraft || !selectedContactId) {
      return;
    }

    try {
      const createdMessage = await sendChatMessage(selectedContactId, trimmedDraft);
      messageIdsRef.current.add(createdMessage.id);
      setMessages((currentMessages) => mergeMessages(currentMessages, [createdMessage]));
      setDraft('');
      setError('');
    } catch (sendError) {
      setError(sendError.message);
    }
  };

  return (
    <div className={styles.chatShell}>
      {isOpen && (
        <section className={styles.panel}>
          <header className={styles.panelHeader}>
            <div className={styles.titleBlock}>
              <h3>Chat TDP TRANSPORT</h3>
              <p>
                {selectedContact
                  ? `Convorbire cu ${selectedContact.fullName}`
                  : `${userFullName} | ${userRoleName}`}
              </p>
            </div>
            <span className={styles.status}>{statusLabels[status] ?? status}</span>
          </header>

          <div className={styles.contactsBar}>
            {isLoadingContacts && (
              <div className={styles.contactsState}>Se incarca conversatiile...</div>
            )}

            {!isLoadingContacts && contactsError && (
              <div className={styles.contactsState}>{contactsError}</div>
            )}

            {!isLoadingContacts && !contactsError && contacts.length === 0 && (
              <div className={styles.contactsState}>
                {adminUser
                  ? 'Nu exista utilizatori disponibili pentru chat privat.'
                  : 'Administratorul nu este disponibil pentru conversatie.'}
              </div>
            )}

            {!isLoadingContacts && !contactsError && contacts.length > 0 && (
              <div className={styles.contactList}>
                {contacts.map((contact) => (
                  <button
                    key={contact.id}
                    type="button"
                    className={`${styles.contactChip} ${selectedContactId === contact.id ? styles.contactChipActive : ''}`}
                    onClick={() => setSelectedContactId(contact.id)}
                  >
                    <span className={styles.contactName}>{contact.fullName}</span>
                    <span className={styles.contactRole}>{contact.roleName}</span>
                    {(unreadByContact[contact.id] ?? 0) > 0 && (
                      <span className={styles.contactUnread}>{unreadByContact[contact.id]}</span>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className={styles.messages}>
            {!contactsError && contacts.length > 0 && !selectedContact && (
              <div className={styles.emptyState}>Selecteaza un utilizator pentru a deschide conversatia.</div>
            )}

            {selectedContact && isLoadingHistory && (
              <div className={styles.emptyState}>Se incarca istoricul conversatiei...</div>
            )}

            {selectedContact && !isLoadingHistory && messages.length === 0 && !error && (
              <div className={styles.emptyState}>Nu exista mesaje in aceasta conversatie. Scrie primul mesaj.</div>
            )}

            {error && <div className={styles.emptyState}>{error}</div>}

            {selectedContact && !isLoadingHistory && messages.map((message) => {
              const isMine = message.senderUserId === user.id;

              return (
                <div
                  key={message.id}
                  className={`${styles.messageRow} ${isMine ? styles.mine : styles.theirs}`}
                >
                  <article className={styles.bubble}>
                    <div className={styles.meta}>
                      <strong>{message.senderName}</strong>
                      <span className={styles.roleTag}>{message.senderRole}</span>
                    </div>
                    <p className={styles.messageText}>{message.message}</p>
                    <div className={styles.meta}>
                      <span>{formatTime(message.timestampUtc)}</span>
                    </div>
                  </article>
                </div>
              );
            })}
            <div ref={messagesEndRef}></div>
          </div>

          <div className={styles.composer}>
            <textarea
              value={draft}
              maxLength={500}
              placeholder={selectedContact ? 'Scrie un mesaj...' : 'Selecteaza mai intai o conversatie...'}
              disabled={!selectedContactId}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' && !event.shiftKey) {
                  event.preventDefault();
                  void handleSend();
                }
              }}
            />
            <button
              type="button"
              className={styles.sendButton}
              disabled={!selectedContactId || !draft.trim()}
              onClick={() => {
                void handleSend();
              }}
            >
              Trimite
            </button>
          </div>
        </section>
      )}

      <button type="button" className={styles.launcher} onClick={handleToggle}>
        <span>{isOpen ? 'Inchide chat' : 'Deschide chat'}</span>
        {unreadCount > 0 && <span className={styles.unread}>{unreadCount}</span>}
      </button>
    </div>
  );
};

export default ChatWidget;
