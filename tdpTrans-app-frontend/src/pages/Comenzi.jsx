import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { fetchActivityLogs, fetchObservations } from '../api/adminApi';
import { createMission, deleteMission, fetchMissions, fetchStatistics, updateMission } from '../api/missionsApi';
import Grafice from '../components/Grafice';
import { missionSchema } from '../utils.js';
import { getStoredUser, isAdminUser } from '../utils/session';
import styles from './Comenzi.module.css';

const emptyMission = {
  id: '',
  missionType: '',
  truckId: '',
  date: '',
  cost: '',
  client: '',
  phone: '',
  email: '',
  address: '',
  missionStatus: '',
};

const formatDateTime = (timestamp) =>
  new Intl.DateTimeFormat('ro-RO', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(timestamp));

function Comenzi() {
  const navigate = useNavigate();
  const [currentUser] = useState(() => getStoredUser());
  const adminUser = isAdminUser(currentUser);
  const [missions, setMissions] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedId, setSelectedId] = useState(null);
  const [isAdding, setIsAdding] = useState(false);
  const [formData, setFormData] = useState(emptyMission);
  const [searchTerm, setSearchTerm] = useState('');
  const [stats, setStats] = useState({ totalComenzi: 0, totalTransportMarfa: 0, totalTractari: 0 });
  const [activityLogs, setActivityLogs] = useState([]);
  const [observations, setObservations] = useState([]);
  const [panelError, setPanelError] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const itemsPerPage = 5;

  useEffect(() => {
    if (!currentUser) {
      navigate('/login');
      return;
    }

    if (!adminUser) {
      alert('Acces interzis: doar administratorii pot accesa panoul operational.');
      navigate('/acasa');
    }
  }, [adminUser, currentUser, navigate]);

  const incarcaDatele = async (page, search = searchTerm) => {
    try {
      setIsLoading(true);
      const data = await fetchMissions(page, itemsPerPage, search);

      if (data) {
        setMissions(data.items);
        setTotalPages(data.totalPages);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const incarcaStatisticile = async () => {
    const data = await fetchStatistics();

    if (data) {
      setStats(data);
    }
  };

  const incarcaPanourileAdmin = async () => {
    try {
      const [logsData, observationsData] = await Promise.all([
        fetchActivityLogs(),
        fetchObservations(),
      ]);

      setActivityLogs(logsData);
      setObservations(observationsData);
      setPanelError('');
    } catch (error) {
      setPanelError(error.message);
    }
  };

  useEffect(() => {
    if (!adminUser) {
      return;
    }

    incarcaStatisticile();
    incarcaPanourileAdmin();
  }, [adminUser]);

  useEffect(() => {
    if (!adminUser) {
      return;
    }

    incarcaDatele(currentPage);
  }, [adminUser, currentPage]);

  useEffect(() => {
    if (!adminUser) {
      return;
    }

    const timer = setTimeout(() => {
      setCurrentPage(1);
      incarcaDatele(1, searchTerm);
    }, 350);

    return () => clearTimeout(timer);
  }, [adminUser, searchTerm]);

  const selectedMission = missions.find((mission) => mission.id === selectedId) || emptyMission;

  const handleCloseModal = () => {
    setSelectedId(null);
    setIsAdding(false);
    setFormData(emptyMission);
  };

  const handleAddNew = () => {
    setSelectedId(null);
    setIsAdding(true);
    setFormData(emptyMission);
  };

  const refreshOperationalData = async () => {
    await Promise.all([
      incarcaDatele(currentPage),
      incarcaStatisticile(),
      incarcaPanourileAdmin(),
    ]);
  };

  const handleDelete = async () => {
    const success = await deleteMission(selectedId);

    if (success) {
      await refreshOperationalData();
      handleCloseModal();
    } else {
      alert('A aparut o eroare la stergerea comenzii.');
    }
  };

  const handleUpdate = async () => {
    try {
      missionSchema.parse(formData);
    } catch (validationError) {
      alert(`Eroare de validare: ${validationError.issues[0].message}`);
      return;
    }

    try {
      const dataToSend = {
        ...formData,
        cost: Number(formData.cost) || 0,
        truckId: Number(formData.truckId) || 0,
        date: formData.date?.trim() ? formData.date : new Date().toISOString(),
      };

      if (isAdding) {
        delete dataToSend.id;
        await createMission(dataToSend);
      } else {
        dataToSend.id = Number(dataToSend.id) || 0;
        await updateMission(dataToSend.id, dataToSend);
      }

      await refreshOperationalData();
      setIsAdding(false);
      alert('Salvat cu succes!');
      handleCloseModal();
    } catch (serverError) {
      alert(`Eroare de la server: ${serverError.message}`);
    }
  };

  const handleChange = (event) => {
    const { name, value } = event.target;
    setFormData((currentValue) => ({ ...currentValue, [name]: value }));
  };

  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', marginTop: '50px' }}>
        <h2>Se incarca datele de pe server...</h2>
      </div>
    );
  }

  return (
    <div className={styles.dashboard}>
      <aside className={styles.sidebar}>
        <div className={styles.sidebarHeader}>
          <h2>TDP TRANSPORT | Comenzi</h2>
        </div>
        <Link to="/acasa" className={styles.homeBtn}>Inapoi Acasa</Link>
        <button className={styles.addBtn} onClick={handleAddNew}>Adauga comanda</button>

        <div className={styles.searchBar}>
          <input
            type="text"
            placeholder="Cauta o comanda..."
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
          />
        </div>

        <div className={styles.masterList}>
          {missions.map((mission) => (
            <div
              key={mission.id}
              className={`${styles.masterItem} ${selectedId === mission.id ? styles.active : ''}`}
              onClick={() => {
                setSelectedId(mission.id);
                setIsAdding(false);
                setFormData(mission);
              }}
            >
              <div className={styles.itemInfo}>
                <span className={styles.itemName}>{mission.client}</span>
                <span className={styles.itemSub}>ID: #{mission.id} | {mission.phone}</span>
              </div>
            </div>
          ))}

          {missions.length === 0 && (
            <p className={styles.noResults}>Nu exista comenzi de afisat.</p>
          )}

          <div className={styles.pagination}>
            <button disabled={currentPage === 1} onClick={() => setCurrentPage((value) => value - 1)}>«</button>
            <span>{currentPage} / {totalPages || 1}</span>
            <button disabled={currentPage >= totalPages} onClick={() => setCurrentPage((value) => value + 1)}>»</button>
          </div>
        </div>
      </aside>

      <main className={styles.mainContent}>
        <div className={styles.mainStack}>
          <Grafice stats={stats} />

          <section className={styles.insightGrid}>
            <article className={styles.insightCard}>
              <div className={styles.insightHeader}>
                <div>
                  <h3>Observation List</h3>
                  <p>Utilizatori marcati automat pentru comportament suspect.</p>
                </div>
                <span className={styles.countBadge}>{observations.length}</span>
              </div>

              {observations.length === 0 ? (
                <p className={styles.emptyState}>Niciun utilizator nu se afla in observatie in acest moment.</p>
              ) : (
                <div className={styles.insightList}>
                  {observations.map((observation) => (
                    <div key={observation.id} className={styles.insightItem}>
                      <div className={styles.insightMeta}>
                        <strong>{observation.userName}</strong>
                        <span className={`${styles.riskBadge} ${observation.riskScore >= 80 ? styles.riskHigh : observation.riskScore >= 65 ? styles.riskMedium : styles.riskLow}`}>
                          risc {observation.riskScore}
                        </span>
                      </div>
                      <div className={styles.insightMeta}>
                        <span className={styles.rolePill}>{observation.groupId}</span>
                        <span>{observation.email}</span>
                      </div>
                      <p>{observation.reason}</p>
                      <p>{observation.details}</p>
                      <small>Ultima actualizare: {formatDateTime(observation.lastDetectedAtUtc)}</small>
                    </div>
                  ))}
                </div>
              )}
            </article>

            <article className={styles.insightCard}>
              <div className={styles.insightHeader}>
                <div>
                  <h3>Activity Stream</h3>
                  <p>Actiunile recente ale utilizatorilor autentificati.</p>
                </div>
                <span className={styles.countBadge}>{activityLogs.length}</span>
              </div>

              {activityLogs.length === 0 ? (
                <p className={styles.emptyState}>Nu exista inca actiuni inregistrate.</p>
              ) : (
                <div className={styles.insightList}>
                  {activityLogs.map((log) => (
                    <div key={log.id} className={styles.insightItem}>
                      <div className={styles.insightMeta}>
                        <strong>{log.userName}</strong>
                        <span className={`${styles.statusBadge} ${log.isSuccess ? styles.logSuccess : styles.logFailed}`}>
                          {log.isSuccess ? 'succes' : 'esuat'}
                        </span>
                      </div>
                      <div className={styles.insightMeta}>
                        <span className={styles.rolePill}>{log.groupId}</span>
                        <span>{log.actionType}</span>
                      </div>
                      <p>{log.actionInformation}</p>
                      <small>{formatDateTime(log.timestampUtc)}</small>
                    </div>
                  ))}
                </div>
              )}
            </article>
          </section>

          {panelError && <div className={styles.panelError}>{panelError}</div>}
        </div>
      </main>

      {(selectedId || isAdding) && (
        <div className={styles.modalOverlay}>
          <div className={styles.detailCard}>
            <button className={styles.closeModalBtn} onClick={handleCloseModal}>✕</button>

            <header className={styles.detailHeader}>
              <div className={styles.avatar}><i className="far fa-user"></i></div>
              <div>
                <h1>{isAdding ? 'Adauga Comanda Noua' : selectedMission.client}</h1>
                <p>Statusul Comenzii: {selectedMission.missionStatus || 'necompletat'}</p>
              </div>
            </header>

            <section className={styles.infoSection}>
              <h3>Informatii Generale</h3>
              <div className={styles.grid}>
                <div><label>Nume Client</label><input name="client" type="text" value={formData.client || ''} onChange={handleChange} /></div>
                <div><label>Nr. de Telefon</label><input name="phone" type="text" value={formData.phone || ''} onChange={handleChange} /></div>
                <div><label>Email</label><input name="email" type="text" value={formData.email || ''} onChange={handleChange} /></div>
                <div>
                  <label>Tip Comanda</label>
                  <select name="missionType" value={formData.missionType || ''} onChange={handleChange}>
                    <option value="">Alege tipul...</option>
                    <option value="Tractare">Tractare</option>
                    <option value="Transport">Transport</option>
                  </select>
                </div>
                <div><label>ID Camion</label><input name="truckId" type="text" value={formData.truckId || ''} onChange={handleChange} /></div>
                <div>
                  <label>Status Comanda</label>
                  <select name="missionStatus" value={formData.missionStatus || ''} onChange={handleChange}>
                    <option value="">Alege statusul...</option>
                    <option value="Programata">Programata</option>
                    <option value="In_desfasurare">In desfasurare</option>
                    <option value="Finalizata">Finalizata</option>
                  </select>
                </div>
              </div>
            </section>

            <section className={styles.infoSection}>
              <h3>Locatia Comenzii si Costul</h3>
              <div className={styles.grid}>
                <div className={styles.fullWidth}><label>Adresa</label><input name="address" type="text" value={formData.address || ''} onChange={handleChange} /></div>
                <div><label>Data</label><input name="date" type="date" value={formData.date ? formData.date.split('T')[0] : ''} onChange={handleChange} /></div>
                <div><label>Cost Total</label><input name="cost" type="text" value={formData.cost || ''} onChange={handleChange} /></div>
              </div>
            </section>

            <footer className={styles.detailFooter}>
              <button className={styles.editBtn} onClick={handleUpdate}>Salveaza</button>
              {selectedId && (
                <button className={styles.deleteBtn} onClick={handleDelete}>Sterge</button>
              )}
              <button className={styles.cancelBtn} onClick={handleCloseModal}>Anuleaza</button>
            </footer>
          </div>
        </div>
      )}
    </div>
  );
}

export default Comenzi;
