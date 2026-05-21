import { Link } from 'react-router-dom';
import PresentationLayout from '../components/PresentationLayout';
import { contactCards, coverageAreas, serviceSteps } from '../data/presentationContent';
import styles from './styles/Presentation.module.css';

const Contact = () => {
  return (
    <PresentationLayout>
      <section className={styles.pageHero}>
        <span className={styles.pageHeroMeta}>Contact si ofertare</span>
        <h1 className={styles.pageHeroTitle}>Trimite rapid detaliile cursei si revenim cu confirmarea.</h1>
        <p className={styles.pageHeroText}>
          Pentru o estimare cat mai corecta, spune-ne ce trebuie transportat sau tractat, de unde se
          preia, unde ajunge si in ce interval doresti cursa.
        </p>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Date utile</span>
          <h2 className={styles.sectionTitle}>Canale rapide de contact si aria de lucru.</h2>
        </div>

        <div className={styles.contactGrid}>
          {contactCards.map((card) => (
            <article key={card.title} className={styles.contactCard}>
              <h3 className={styles.cardTitle}>{card.title}</h3>
              <span className={styles.contactValue}>{card.value}</span>
              <p>{card.description}</p>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.featureSplit}>
        <article className={styles.splitPanel}>
          <h2 className={styles.panelTitle}>Ce e bine sa incluzi in cerere</h2>
          <div className={styles.highlightList}>
            <div className={styles.highlightItem}>
              <span className={styles.highlightDot}></span>
              <span>Tipul serviciului: transport marfa sau tractare auto.</span>
            </div>
            <div className={styles.highlightItem}>
              <span className={styles.highlightDot}></span>
              <span>Adresa de preluare, destinatia si intervalul dorit.</span>
            </div>
            <div className={styles.highlightItem}>
              <span className={styles.highlightDot}></span>
              <span>Dimensiuni aproximative, greutate sau particularitati de acces.</span>
            </div>
          </div>
        </article>

        <article className={styles.splitPanel}>
          <h2 className={styles.panelTitle}>Zone acoperite</h2>
          <ul className={styles.coverageList}>
            {coverageAreas.map((area) => (
              <li key={area}>{area}</li>
            ))}
          </ul>
        </article>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Pasii de lucru</span>
          <h2 className={styles.sectionTitle}>Cum decurge colaborarea dupa ce ne contactezi.</h2>
        </div>

        <div className={styles.timeline}>
          {serviceSteps.map((step) => (
            <article key={step.step} className={styles.timelineItem}>
              <span className={styles.timelineStep}>{step.step}</span>
              <h3 className={styles.cardTitle}>{step.title}</h3>
              <p>{step.description}</p>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.quoteBanner}>
        <div className={styles.quoteText}>
          <h2 className={styles.quoteTitle}>Vrei sa pleci de la un cost estimativ?</h2>
          <p>
            Poti folosi calculatorul de pret pentru o valoare orientativa, apoi ne scrii pentru
            confirmarea finala a traseului si disponibilitatii.
          </p>
        </div>
        <div className={styles.quoteActions}>
          <Link to="/calculator-pret" className={styles.primaryButton}>
            Deschide calculatorul
          </Link>
        </div>
      </section>
    </PresentationLayout>
  );
};

export default Contact;
