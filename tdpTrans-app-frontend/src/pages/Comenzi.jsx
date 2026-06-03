import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { fetchActivityLogs, fetchObservations, fetchSecurityStatistics, seedSecurityLab } from '../api/adminApi';
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

const normalizeMissionDateForApi = (value) => {
  const trimmedValue = value?.trim() ?? '';

  if (!trimmedValue) {
    return new Date().toISOString();
  }

  if (/^\d{4}-\d{2}-\d{2}$/.test(trimmedValue)) {
    return `${trimmedValue}T00:00:00Z`;
  }

  return trimmedValue;
};

const getRiskBadgeClass = (riskScore) => {
  if (riskScore >= 80) {
    return styles.riskHigh;
  }

  if (riskScore >= 60) {
    return styles.riskMedium;
  }

  return styles.riskLow;
};

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
  const [securityBenchmark, setSecurityBenchmark] = useState({ optimized: null, naive: null });
  const [seedSummary, setSeedSummary] = useState(null);
  const [panelError, setPanelError] = useState('');
  const [isBenchmarkLoading, setIsBenchmarkLoading] = useState(false);
  const [isSeedingSecurityLab, setIsSeedingSecurityLab] = useState(false);
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
      setIsBenchmarkLoading(true);
      const [logsData, observationsData, optimizedStats, naiveStats] = await Promise.all([
        fetchActivityLogs(),
        fetchObservations(),
        fetchSecurityStatistics('optimized', 24),
        fetchSecurityStatistics('naive', 24),
      ]);
      setActivityLogs(logsData);
      setObservations(observationsData);
      setSecurityBenchmark({
        optimized: optimizedStats,
        naive: naiveStats,
      });
      setPanelError('');
    } catch (error) {
      setPanelError(error.message);
    } finally {
      setIsBenchmarkLoading(false);
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

  const handleSeedSecurityLab = async () => {
    try {
      setPanelError('');
      setIsSeedingSecurityLab(true);
      const response = await seedSecurityLab();
      setSeedSummary(response);
      await refreshOperationalData();
    } catch (error) {
      setPanelError(error.message);
    } finally {
      setIsSeedingSecurityLab(false);
    }
  };

  const optimizedBenchmark = securityBenchmark.optimized;
  const naiveBenchmark = securityBenchmark.naive;
  const benchmarkSpeedup = optimizedBenchmark && naiveBenchmark && optimizedBenchmark.durationMs > 0
    ? (naiveBenchmark.durationMs / optimizedBenchmark.durationMs).toFixed(1)
    : null;

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
        date: normalizeMissionDateForApi(formData.date),
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

            <article className={styles.insightCard}>
              <div className={styles.insightHeader}>
                <div>
                  <h3>Suspicious Activity</h3>
                  <p>Utilizatori marcati in timp real de motorul de risc pe baza regulilor si de detectorul AI local pentru tentative esuate, acces refuzat, flood pe chat sau drift intre sesiuni.</p>
                </div>
                <span className={styles.countBadge}>{observations.length}</span>
              </div>

              {observations.length === 0 ? (
                <p className={styles.emptyState}>Nu exista observatii active in acest moment.</p>
              ) : (
                <div className={styles.insightList}>
                  {observations.map((observation) => (
                    <div key={observation.id} className={styles.insightItem}>
                      <div className={styles.insightMeta}>
                        <strong>{observation.userName}</strong>
                        <span className={`${styles.riskBadge} ${getRiskBadgeClass(observation.riskScore)}`}>
                          risc {observation.riskScore}
                        </span>
                      </div>
                      <div className={styles.insightMeta}>
                        <span className={styles.rolePill}>{observation.groupId}</span>
                        <span>{observation.reason}</span>
                      </div>
                      <p>{observation.details}</p>
                      <small>Ultima detectie: {formatDateTime(observation.lastDetectedAtUtc)}</small>
                    </div>
                  ))}
                </div>
              )}
            </article>

            <article className={styles.insightCard}>
              <div className={styles.insightHeader}>
                <div>
                  <h3>Security Lab</h3>
                  <p>Statistica gold construita peste relatia multi-la-multi dintre roluri si permisiuni, plus detectorul AI local, cu varianta naiva si varianta optimizata prin indecsi si cache.</p>
                </div>
                <span className={styles.countBadge}>{optimizedBenchmark?.topRiskUsers?.length ?? 0}</span>
              </div>

              <div className={styles.insightActions}>
                <button className={styles.editBtn} disabled={isSeedingSecurityLab} onClick={handleSeedSecurityLab}>
                  {isSeedingSecurityLab ? 'Se genereaza datele...' : 'Genereaza date de test'}
                </button>
                {benchmarkSpeedup && (
                  <span className={styles.secondaryBtn}>Optimizat x{benchmarkSpeedup}</span>
                )}
              </div>

              {seedSummary && (
                <p className={styles.subtleText}>
                  Ultimul seed: {seedSummary.createdUsers} utilizatori, {seedSummary.createdMissions} misiuni, {seedSummary.createdActivityLogs} loguri, {seedSummary.suspiciousProfilesSeeded} profile suspecte.
                </p>
              )}

              {isBenchmarkLoading && !optimizedBenchmark ? (
                <p className={styles.emptyState}>Se calculeaza benchmark-ul de securitate...</p>
              ) : optimizedBenchmark && naiveBenchmark ? (
                <>
                  <div className={styles.metricsGrid}>
                    <div className={styles.metricCard}>
                      <span className={styles.metricLabel}>Naiv</span>
                      <strong className={styles.metricValue}>{naiveBenchmark.durationMs} ms</strong>
                    </div>
                    <div className={styles.metricCard}>
                      <span className={styles.metricLabel}>Optimizat</span>
                      <strong className={styles.metricValue}>{optimizedBenchmark.durationMs} ms</strong>
                    </div>
                    <div className={styles.metricCard}>
                      <span className={styles.metricLabel}>Utilizatori</span>
                      <strong className={styles.metricValue}>{optimizedBenchmark.totalUsers}</strong>
                    </div>
                    <div className={styles.metricCard}>
                      <span className={styles.metricLabel}>Muchii permisiuni</span>
                      <strong className={styles.metricValue}>{optimizedBenchmark.totalPermissionAssignments}</strong>
                    </div>
                  </div>

                  <div className={styles.insightList}>
                    {optimizedBenchmark.permissionStatistics.slice(0, 3).map((statistic) => (
                      <div key={statistic.permissionName} className={styles.insightItem}>
                        <div className={styles.insightMeta}>
                          <strong>{statistic.permissionName}</strong>
                          <span className={styles.rolePill}>{statistic.userCount} utilizatori</span>
                        </div>
                        <p>
                          {statistic.assignmentEdges} asignari, {statistic.successfulActionCount} actiuni reusite, {statistic.failedActionCount} esuate,
                          {' '}{statistic.observedRiskUsers} utilizatori cu risc observat.
                        </p>
                      </div>
                    ))}
                  </div>

                  <div className={styles.insightList}>
                    {optimizedBenchmark.topRiskUsers.slice(0, 3).map((user) => (
                      <div key={user.userId} className={styles.insightItem}>
                        <div className={styles.insightMeta}>
                          <strong>{user.fullName}</strong>
                          <span className={`${styles.riskBadge} ${getRiskBadgeClass(user.riskScore)}`}>
                            risc {user.riskScore}
                          </span>
                        </div>
                        <p>
                          {user.failedLogins} login-uri esuate, {user.permissionDenials} accesari refuzate, {user.chatMessagesLastTwoMinutes} mesaje pe chat,
                          {' '}{user.distinctRecentIpCount} IP-uri recente.
                        </p>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <p className={styles.emptyState}>Benchmark-ul de securitate nu este disponibil inca.</p>
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
