import { useState } from 'react';
import { Link } from 'react-router-dom';
import styles from './Comenzi.module.css';
import { getPaginatedItems } from '../utils.js'
import { missionsStore } from '../store/missionsStore.js';
import Grafice from '../components/Grafice';


function Comenzi() {
  const [missions, setMissions] = useState(missionsStore.getAll());
  const [selectedId, setSelectedId] = useState(null);
  const [isAdding, setIsAdding] = useState(false);
  const [formData, setFormData] = useState({});
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 4;

  const selectedMission = missions.find(mission => mission.id === selectedId) || {
    id: '', type: '', truckId: '', date: '', cost: '', client: '', phone: '', address: ''
  };

  const handleCloseModal = () => {
    setSelectedId(null);
    setIsAdding(false);
  };
  
  const handleAddNew = () => {
    setSelectedId(null);
    setIsAdding(true);
    setFormData({ id: '', type: '', truckId: '', date: '', cost: '', client: '', phone: '', email: '', address: '', status: 'Noua' })
  };

  const handleDelete = () => {
    const updatedList = missionsStore.deleteMission(selectedId);
    setMissions(updatedList);
    setSelectedId(null);
    handleCloseModal();
  };

  const handleUpdate = () => {
    console.log("Date trimise la validare: ", formData);

    try {
      const missionResult = missionsStore.saveMission(formData, isAdding);

      setMissions(missionsStore.getAll());
      setIsAdding(false);
      setSelectedId(missionResult.id);
      alert("Salvat cu succes!");
      handleCloseModal();
    } catch (err) {
      alert(err.message);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value});
  }

  const filteredMissions = missions.filter(mission => 
    mission.client.toLowerCase().includes(searchTerm.toLowerCase()) ||
    mission.phone.includes(searchTerm) ||
    mission.id.toString().includes(searchTerm)
  );

  const { currentItems, totalPages } = getPaginatedItems(filteredMissions, currentPage, itemsPerPage);


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
          {currentItems.map(mission => (
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

          {currentItems.length === 0 && (
            <p className={styles.noResults}>Nu am gasit niciun client cu acest nume...</p>
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
        <Grafice /> 
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
                <p>Statusul Comenzii: {selectedMission.status}</p>
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
                  <select name='type' value={formData.type || ''} onChange={handleChange}>
                    <option value="">Alege tipul...</option>
                    <option value="Tractare">Tractare</option>
                    <option value="Transport">Transport</option>
                  </select>
                </div>
                <div><label>ID Camion</label><input name='truckId' type="text" value={formData.truckId || ''} onChange={handleChange} /></div>
              </div>
            </section>

            <section className={styles.infoSection}>
              <h3>Locația Comenzii & Costul</h3>
              <div className={styles.grid}>
                <div className={styles.fullWidth}><label>Adresa</label><input name='address' type="text" value={formData.address || ''} onChange={handleChange} /></div>
                <div><label>Data</label><input name='date' type="date" value={formData.date || ''} onChange={handleChange} /></div>
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
