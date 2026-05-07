import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import styles from './Comenzi.module.css';
import { missionSchema } from '../utils.js'
import Grafice from '../components/Grafice';
import { fetchMissions, createMission, updateMission, deleteMission, fetchStatistics } from '../api/missionsApi';

function Comenzi() {
  const [missions, setMissions] = useState([]); 
  const [isLoading, setIsLoading] = useState(true);
  const [selectedId, setSelectedId] = useState(null);
  const [isAdding, setIsAdding] = useState(false);
  const [formData, setFormData] = useState({});
  const [searchTerm, setSearchTerm] = useState('');
  const [stats, setStats] = useState({ totalComenzi: 0, totalTransportMarfa: 0, totalTractari: 0 });
  
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const itemsPerPage = 5;

  const incarcaDatele = async (page, search = searchTerm) => {
    setIsLoading(true);
    const data = await fetchMissions(page, itemsPerPage, search);
    
    if (data) {
      setMissions(data.items); 
      setTotalPages(data.totalPages);
    }
    setIsLoading(false);
  };

  const incarcaStatisticile = async () => {
        const data = await fetchStatistics();
        if (data) {
            setStats(data);
        }
    };

  useEffect(() => {
    incarcaStatisticile();
  }, []);
  
  useEffect(() => {
    incarcaDatele(currentPage);
  }, [currentPage]);

  useEffect(() => {
    const timer = setTimeout(() => {
        setCurrentPage(1);
        incarcaDatele(1, searchTerm);
    }, 500);

    return () => clearTimeout(timer);
  }, [searchTerm]);

  const selectedMission = missions.find(mission => mission.id === selectedId) || {
    id: '', missionType: '', truckId: '', date: '', cost: '', client: '', phone: '', address: '', missionStatus: ''
  };

  const handleCloseModal = () => {
    setSelectedId(null);
    setIsAdding(false);
  };
  
  const handleAddNew = () => {
    setSelectedId(null);
    setIsAdding(true);
    setFormData({ id: '', missionType: '', truckId: '', date: '', cost: '', client: '', phone: '', address: '', missionStatus: '' });
  };

  const handleDelete = async () => {
    const success = await deleteMission(selectedId);
    
    if (success) {
        await incarcaDatele(currentPage);
        await incarcaStatisticile();
        setSelectedId(null);
        handleCloseModal();
    } else {
        alert("A apărut o eroare la ștergerea comenzii.");
    }
  };

  const handleUpdate = async () => {
    try {
      missionSchema.parse(formData);
    } catch (err) {
      alert("Eroare de validare: " + err.issues[0].message);
      return;
    }

    try {
      const dataToSend = { ...formData };
      
      dataToSend.cost = Number(dataToSend.cost) || 0;
      dataToSend.truckId = Number(dataToSend.truckId) || 0;
      if (!dataToSend.date || dataToSend.date.trim() === '') {
        dataToSend.date = new Date().toISOString();
      }
      
      if (isAdding) {
        delete dataToSend.id;
        console.log("Date trimise la POST (fără ID): ", dataToSend);
        await createMission(dataToSend);
      } else {
        dataToSend.id = Number(dataToSend.id) || 0;
        console.log("Date trimise la PUT (cu ID): ", dataToSend);
        await updateMission(dataToSend.id, dataToSend);
      }

      await incarcaDatele(currentPage);
      await incarcaStatisticile();
      
      setIsAdding(false);
      alert("Salvat cu succes!");
      handleCloseModal();
    } catch (err) {
      alert("Eroare de la server: " + err.message);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value});
  }

  if (isLoading) {
    return <div style={{ textAlign: 'center', marginTop: '50px' }}><h2>Se încarcă datele de pe server... 🚚</h2></div>;
  }

  return (
    <div className={styles.dashboard}>
      
      {/* sidebar (partea de master - lista de comenzi) */}
      <aside className={styles.sidebar}>
        <div className={styles.sidebarHeader}>
          <h2>TDP TRANS | Comenzi</h2>
        </div>
        <Link to="/acasa" className={styles.homeBtn}>Inapoi Acasa</Link>
        <button className={styles.addBtn} onClick={handleAddNew}>Adauga comanda</button>

        <div className={styles.searchBar}>
          <input
            type="text"
            placeholder="Cauta o comanda..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        
        <div className={styles.masterList}>
          {missions.map(mission => (
            <div
              key={mission.id}
              className={`${styles.masterItem} ${selectedId === mission.id ? styles.active : ''}`}
              onClick={() => { setSelectedId(mission.id); setIsAdding(false); setFormData(mission) }}
            >
              <div className={styles.itemInfo}>
                <span className={styles.itemName}>{mission.client}</span>
                <span className={styles.itemSub}>ID: #{mission.id} | {mission.phone}</span>
              </div>
            </div>
          ))}

          {missions.length === 0 && (
            <p className={styles.noResults}>Nu exista comenzi de afisat...<br></br>Ne pare rău!</p>
          )}

          <div className={styles.pagination}>
            <button disabled={currentPage === 1} onClick={() => setCurrentPage(currentPage - 1)}>«</button>
            <span>{currentPage} / {totalPages || 1}</span>
            <button disabled={currentPage >= totalPages} onClick={() => setCurrentPage(currentPage + 1)}>»</button>
          </div>
        </div>
      </aside>

      {/* zona principala (charts) */}
      <main className={styles.mainContent}>
        <Grafice stats={stats} /> 
      </main>

      {/* modalul pentru formular (partea de detail) */}
      {(selectedId || isAdding) && (
        <div className={styles.modalOverlay}>
          <div className={styles.detailCard}>
            
            <button className={styles.closeModalBtn} onClick={handleCloseModal}>✖</button>

            <header className={styles.detailHeader}>
              <div className={styles.avatar}><i className="far fa-user"></i></div>
              <div>
                <h1>{isAdding ? "Adaugă Comandă Nouă" : selectedMission.client}</h1>
                <p>Statusul Comenzii: {selectedMission.missionStatus}</p>
              </div>
            </header>

            <section className={styles.infoSection}>
              <h3>Informații Generale</h3>
              <div className={styles.grid}>
                <div><label>Nume Client</label><input name='client' type="text" value={formData.client || ''} onChange={handleChange} /></div>
                <div><label>Nr. de Telefon</label><input name='phone' type="text" value={formData.phone || ''} onChange={handleChange} /></div>
                <div><label>Email</label><input name='email' type="text" value={formData.email || ''} onChange={handleChange} /></div>
                <div>
                  <label>Tip Comanda</label>
                  <select name='missionType' value={formData.missionType || ''} onChange={handleChange}>
                    <option value="">Alege tipul...</option>
                    <option value="Tractare">Tractare</option>
                    <option value="Transport">Transport</option>
                  </select>
                </div>
                <div><label>ID Camion</label><input name='truckId' type="text" value={formData.truckId || ''} onChange={handleChange} /></div>
                <div>
                  <label>Status Comanda</label>
                  <select name='missionStatus' value={formData.missionStatus || ''} onChange={handleChange}>
                    <option value="">Alege statusul...</option>
                    <option value="Programata">Programata</option>
                    <option value="In_desfasurare">In desfasurare</option>
                    <option value="Finalizata">Finalizata</option>
                  </select>
                </div>
              </div>
            </section>

            <section className={styles.infoSection}>
              <h3>Locația Comenzii & Costul</h3>
              <div className={styles.grid}>
                <div className={styles.fullWidth}><label>Adresa</label><input name='address' type="text" value={formData.address || ''} onChange={handleChange} /></div>
                <div><label>Data</label><input name='date' type="date" value={formData.date ? formData.date.split('T')[0] : ''} onChange={handleChange} /></div>
                <div><label>Cost Total</label><input name='cost' type="text" value={formData.cost || ''} onChange={handleChange} /></div>
              </div>
            </section>

            <footer className={styles.detailFooter}>
              <button className={styles.editBtn} onClick={handleUpdate}>Salvează</button>
              {selectedId && (
                <button className={styles.deleteBtn} onClick={handleDelete}>Șterge</button>
              )}
              <button className={styles.cancelBtn} onClick={handleCloseModal}>Anulează</button>
            </footer>
          </div>
        </div>
      )}

    </div>
  );
}

export default Comenzi;