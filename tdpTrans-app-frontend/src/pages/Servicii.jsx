import { Link } from 'react-router-dom';
import PresentationLayout from '../components/PresentationLayout';
import { companyHighlights, serviceHighlights, serviceSteps } from '../data/presentationContent';
import styles from './styles/Presentation.module.css';

const Servicii = () => {
  return (
    <PresentationLayout>
      <section className={styles.pageHero}>
        <span className={styles.pageHeroMeta}>Servicii operationale</span>
        <h1 className={styles.pageHeroTitle}>Transport si tractari adaptate la fiecare cursa.</h1>
        <p className={styles.pageHeroText}>
          TDP TRANSPORT livreaza curse dedicate pentru marfa de pana la 3.5 tone si servicii de
          tractare auto, cu accent pe disponibilitate, executie sigura si comunicare directa.
        </p>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Ce acoperim</span>
          <h2 className={styles.sectionTitle}>Pachete de servicii gandite pentru cerinte reale.</h2>
          <p className={styles.sectionLead}>
            Fie ca ai nevoie de o cursa dedicata, de mutarea unor bunuri voluminoase sau de tractarea
            unui vehicul, alegem varianta care se potriveste cel mai bine traseului si timpului tau.
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
            </article>
          ))}
        </div>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Cum lucram</span>
          <h2 className={styles.sectionTitle}>Proces simplu, fara pasi inutili.</h2>
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

      <section className={styles.featureSplit}>
        <article className={styles.splitPanel}>
          <h2 className={styles.panelTitle}>De ce ne aleg clientii</h2>
          <div className={styles.highlightList}>
            {companyHighlights.map((highlight) => (
              <div key={highlight} className={styles.highlightItem}>
                <span className={styles.highlightDot}></span>
                <span>{highlight}</span>
              </div>
            ))}
          </div>
        </article>

        <article className={styles.splitPanel}>
          <h2 className={styles.panelTitle}>Cand te ajuta cel mai mult</h2>
          <p className={styles.splitText}>
            Pagina de servicii este punctul de plecare daca vrei sa identifici rapid tipul de cursa
            potrivit. Pentru cost estimativ poti continua cu pagina de calculator, iar pentru detalii
            operative si disponibilitate poti merge direct in contact.
          </p>
          <div className={styles.actionRow}>
            <Link to="/calculator-pret" className={styles.primaryButton}>
              Vezi calculatorul
            </Link>
            <Link to="/contact" className={styles.secondaryButton}>
              Cere detalii
            </Link>
          </div>
        </article>
      </section>
    </PresentationLayout>
  );
};

export default Servicii;
