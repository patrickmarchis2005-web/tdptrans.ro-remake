import { Link, useNavigate } from 'react-router-dom';
import Comenzi from '../pages/Comenzi.jsx'
import styles from './Acasa.module.css';


const Acasa = () => {
    const navigate = useNavigate();
    
    const userRole = localStorage.getItem('rol');

    const handleMergiLaComenzi = () => {
        if (userRole === 'admin') {
            navigate('/comenzi');
        } else {
            alert("Acces interzis: Doar administratorii pot accesa panoul de comenzi!");
        }
    };

    return (
        <div>
            <div className={styles.pageWrapper}>
                {/* HEADER / NAVBAR */}
                <header className={styles.header}>
                    {/* RÂNDUL 1: Navigația de sus */}
                    <div className={styles.topBar}>
                    <div className={styles.navLeft}>
                        <span className={styles.domain}>tdptrans.ro</span>
                    </div>
                    <nav className={styles.navRight}>
                        <a href="#servicii">Servicii</a>
                        <a href="#calculator">Calculator Pret</a>
                        <a href="#flota">Flota Noastra</a>
                        <a href="#contact">Contact</a>
                        <button className={styles.navButton}>Apeleaza cu incredere!</button>
                    </nav>
                    </div>

                    {/* RÂNDUL 2: Logo și Titlu mare */}
                    <div className={styles.mainHeader}>
                    <h1 className={styles.mainTitle}>TDP TRANS</h1>
                    <p className={styles.subSubtitle}>Transport Marfa | Tractari Auto</p>
                    </div>
                </header>

                {/* HERO SECTION - Imaginea cu mașinile */}
                <section className={styles.heroSection}>
                    <div className={styles.orangeDivider}></div>
                    <img src="/flota_tdptrans.jpg" alt="Flota TDP Trans" className={styles.heroImage} />
                    <div className={styles.orangeDivider}></div>
                </section>

                {/* INTRO SECTION - Logo rotund și text scurt */}
                <section className={styles.introSection}>
                    <div className={styles.introLogoContainer}>
                    <img src="/tdptrans_logo.png" alt="Logo" className={styles.roundLogo} />
                    </div>
                    <div className={styles.introText}>
                    <h2>Soluția ta pentru transport marfă până la 3.5t și tractări auto. <br></br>
                        Firmă cu licență de transport, garantăm servicii de cea mai bună 
                        calitate la cele mai bune prețuri.</h2>
                    </div>
                </section>
                <div className={styles.orangeDivider}></div>

                {/* CONTENT SECTION - Bun venit */}
                <section className={styles.contentSection}>
                    <div className={styles.welcomeText}>
                    <h2>Bun venit!</h2>
                    <p>TDP Trans SRL este o companie din municipiul Cluj-Napoca care activează
                        în domeniul transportului de mărfuri cu autoutilitare de până la 3.5 tone
                        și a tractărilor auto. Vă oferim cele mai bune servicii de transport
                        marfă, mutări și tractări la prețuri accesibile.</p><br></br>
                    <p>Dispunem de mai multe autoutilitare cu prelată de până în 3.5 tone, 
                        cu următoarele dimensiuni ale spațiului de încărcare: 
                        Lungime: 4.20m, Lățime: 2.10m, Înălțime: 2.20m (8 europaleți):</p>
                    <ul>
                        <li>Oferim servicii de tractare pentru autoturisme, autoutilitare sau utilaje, 
                        cu platformă de 6 metri lungime.</li>
                        <li>Oferim servicii personalizate în funcție de nevoile dumneavoastră.</li>
                        <li>Garantăm servicii de calitate și profesionalism în munca depusă.</li>
                        <li>Dispunem de asigurare CMR pentru marfa transportată.</li>
                        <li>Avem mai multe autoutilitare la dispoziție astfel putem fi mereu la dispoziția clientului.</li>
                    </ul>
                    </div>
                    
                    <button 
                        onClick={handleMergiLaComenzi} 
                        className={styles.ordersBtn}
                        style={{
                            opacity: userRole !== 'admin' ? 0.5 : 1,
                            cursor: userRole !== 'admin' ? 'not-allowed' : 'pointer'
                        }}
                    >
                        Administrare Comenzi
                    </button>
                    
                </section>
                <div className={styles.orangeDivider}></div>

                {/* FOOTER */}
                <footer className={styles.footer}>
                    <p>TDP TRANS: Îți purtăm poverile!</p>
                    <p>TDP TRANS: We carry your weight!</p>
                </footer>
            </div>
        </div>
    );
};


export default Acasa;

