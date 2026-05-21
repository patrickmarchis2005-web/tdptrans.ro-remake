import { Link, useNavigate } from 'react-router-dom';
import PresentationLayout from '../components/PresentationLayout';
import {
  companyHighlights,
  estimateExamples,
  fleetCards,
  serviceHighlights,
} from '../data/presentationContent';
import { getStoredUser, isAdminUser } from '../utils/session';
import styles from './styles/Presentation.module.css';

const pageCards = [
  {
    title: 'Calculator Pret',
    description: 'Obtii rapid o estimare orientativa in functie de distanta si urgenta.',
    link: '/calculator-pret',
    cta: 'Calculeaza',
  },
  {
    title: 'Flota Noastra',
    description: 'Vezi ce configuratii folosim pentru transport marfa si tractari auto.',
    link: '/flota',
    cta: 'Descopera flota',
  },
  {
    title: 'Contact',
    description: 'Trimite cererea de oferta si verificam disponibilitatea potrivita pentru cursa.',
    link: '/contact',
    cta: 'Solicita oferta',
  },
];

const Acasa = () => {
  const navigate = useNavigate();
  const currentUser = getStoredUser();
  const adminUser = isAdminUser(currentUser);

  const handleMergiLaComenzi = () => {
    if (adminUser) {
      navigate('/comenzi');
      return;
    }

    alert('Acces interzis: doar administratorii pot accesa panoul de comenzi.');
  };

  return (
    <PresentationLayout>
      <section className={styles.hero}>
        <article className={styles.heroCopy}>
          <span className={styles.eyebrow}>Cluj-Napoca | Transport dedicat</span>
          <h1 className={styles.heroTitle}>TDP TRANSPORT</h1>
          <p className={styles.heroText}>
            Solutia rapida pentru transport marfa de pana la 3.5t si tractari auto, cu raspuns clar,
            disponibilitate flexibila si curse executate in siguranta.
          </p>

          <div className={styles.actionRow}>
            <Link to="/servicii" className={styles.primaryButton}>
              Vezi serviciile
            </Link>
            <Link to="/contact" className={styles.secondaryButton}>
              Cere oferta
            </Link>
          </div>
        </article>

        <article className={styles.heroMedia}>
          <img src="/flota_tdptrans.jpg" alt="Flota TDP TRANSPORT" className={styles.heroImage} />
          <span className={styles.heroBadge}>Transport marfa | Tractari auto | Curse flexibile</span>
        </article>
      </section>

      <section className={styles.statsGrid}>
        <article className={styles.statCard}>
          <span className={styles.statValue}>3.5t</span>
          <span className={styles.statLabel}>Capacitate pentru curse dedicate si marfa voluminoasa.</span>
        </article>
        <article className={styles.statCard}>
          <span className={styles.statValue}>8</span>
          <span className={styles.statLabel}>Europaleti in configuratia standard cu prelata.</span>
        </article>
        <article className={styles.statCard}>
          <span className={styles.statValue}>6m</span>
          <span className={styles.statLabel}>Platforma pentru tractari auto si preluari controlate.</span>
        </article>
        <article className={styles.statCard}>
          <span className={styles.statValue}>CMR</span>
          <span className={styles.statLabel}>Asigurare pentru marfa transportata si operare autorizata.</span>
        </article>
      </section>

      <section className={styles.introBand}>
        <div className={styles.logoCard}>
          <img src="/tdptrans_logo.png" alt="Logo TDP TRANSPORT" className={styles.roundLogo} />
        </div>

        <article className={styles.introCard}>
          <span className={styles.sectionKicker}>Despre noi</span>
          <h2 className={styles.sectionTitle}>Servicii orientate spre timp, siguranta si claritate.</h2>
          <p className={styles.sectionLead}>
            TDP TRANSPORT SRL activeaza in Cluj-Napoca si ofera servicii de transport marfa, mutari
            si tractari auto la preturi competitive. Lucram direct, fara pasi inutili, iar fiecare
            cursa este configurata in functie de volum, traseu si urgenta.
          </p>
        </article>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Servicii principale</span>
          <h2 className={styles.sectionTitle}>Paginile din prezentare sunt acum implementate separat.</h2>
          <p className={styles.sectionLead}>
            Din pagina principala poti intra direct in sectiunile dedicate pentru servicii, pret,
            flota si contact, fara sa pierzi accesul la functionalitatile existente ale aplicatiei.
          </p>
        </div>

        <div className={styles.cardGrid}>
          {serviceHighlights.map((service) => (
            <article key={service.title} className={styles.infoCard}>
              <h3 className={styles.cardTitle}>{service.title}</h3>
              <p className={styles.cardText}>{service.description}</p>
              <ul className={styles.bulletList}>
                {service.bullets.map((bullet) => (
                  <li key={bullet}>{bullet}</li>
                ))}
              </ul>
              <Link to="/servicii" className={styles.cardLink}>
                Vezi detalii
              </Link>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.featureSplit}>
        <article className={styles.splitPanel}>
          <h2 className={styles.panelTitle}>De ce functioneaza bine pentru client</h2>
          <div className={styles.highlightList}>
            {companyHighlights.map((highlight) => (
              <div key={highlight} className={styles.highlightItem}>
                <span className={styles.highlightDot}></span>
                <span>{highlight}</span>
              </div>
            ))}
          </div>
        </article>

        <article className={styles.adminPanel}>
          <h2 className={styles.panelTitle}>Administrare comenzi</h2>
          <p className={styles.helperText}>
            Accesul catre modulul operational ramane disponibil doar pentru administrator, exact ca in
            implementarea existenta.
          </p>
          <button
            type="button"
            onClick={handleMergiLaComenzi}
            className={`${styles.primaryButton} ${!adminUser ? styles.disabledAction : ''}`}
          >
            Deschide panoul de comenzi
          </button>
          <p className={styles.helperText}>
            {adminUser
              ? 'Rol detectat: administrator.'
              : 'Rol detectat: utilizator standard. Pentru acces este necesar un cont admin.'}
          </p>
        </article>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Mai multe pagini</span>
          <h2 className={styles.sectionTitle}>Home page-ul trimite acum spre pagini reale de prezentare.</h2>
        </div>

        <div className={styles.cardGrid}>
          {pageCards.map((page) => (
            <article key={page.title} className={styles.infoCard}>
              <h3 className={styles.cardTitle}>{page.title}</h3>
              <p className={styles.cardText}>{page.description}</p>
              <Link to={page.link} className={styles.cardLink}>
                {page.cta}
              </Link>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Repere utile</span>
          <h2 className={styles.sectionTitle}>Cateva orientari rapide pentru pret si flota.</h2>
        </div>

        <div className={styles.featureSplit}>
          <article className={styles.splitPanel}>
            <h3 className={styles.cardTitle}>{estimateExamples[0].label}</h3>
            <p className={styles.priceValue}>{estimateExamples[0].value}</p>
            <p className={styles.cardText}>{estimateExamples[0].details}</p>
            <Link to="/calculator-pret" className={styles.secondaryButton}>
              Vezi estimatorul complet
            </Link>
          </article>

          <article className={styles.splitPanel}>
            <h3 className={styles.cardTitle}>{fleetCards[0].title}</h3>
            <p className={styles.dimensions}>{fleetCards[0].dimensions}</p>
            <p className={styles.cardText}>{fleetCards[0].description}</p>
            <Link to="/flota" className={styles.secondaryButton}>
              Vezi toate configuratiile
            </Link>
          </article>
        </div>
      </section>
    </PresentationLayout>
  );
};

export default Acasa;
