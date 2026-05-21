import { Link } from 'react-router-dom';
import PresentationLayout from '../components/PresentationLayout';
import { coverageAreas, fleetCards } from '../data/presentationContent';
import styles from './styles/Presentation.module.css';

const Flota = () => {
  return (
    <PresentationLayout>
      <section className={styles.pageHero}>
        <span className={styles.pageHeroMeta}>Flota si configuratii</span>
        <h1 className={styles.pageHeroTitle}>Vehicule gandite pentru curse flexibile si sigure.</h1>
        <p className={styles.pageHeroText}>
          Selectam configuratia potrivita pentru marfa transportata sau pentru tipul de tractare, astfel
          incat fiecare cursa sa ramana eficienta si usor de coordonat.
        </p>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Disponibilitate</span>
          <h2 className={styles.sectionTitle}>Prezentare rapida a flotei TDP TRANSPORT.</h2>
        </div>

        <div className={styles.fleetGrid}>
          {fleetCards.map((vehicle) => (
            <article key={vehicle.title} className={styles.fleetCard}>
              <h3 className={styles.cardTitle}>{vehicle.title}</h3>
              <span className={styles.dimensions}>{vehicle.dimensions}</span>
              <p className={styles.cardText}>{vehicle.description}</p>
              <div className={styles.tagRow}>
                {vehicle.tags.map((tag) => (
                  <span key={tag} className={styles.tag}>
                    {tag}
                  </span>
                ))}
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.featureSplit}>
        <article className={styles.splitPanel}>
          <h2 className={styles.panelTitle}>Zone si tipuri de curse</h2>
          <ul className={styles.coverageList}>
            {coverageAreas.map((area) => (
              <li key={area}>{area}</li>
            ))}
          </ul>
        </article>

        <article className={styles.splitPanel}>
          <h2 className={styles.panelTitle}>Cum alegem vehiculul potrivit</h2>
          <p className={styles.splitText}>
            Ne uitam la volum, greutate, tipul de acces la incarcare si urgenta. Pentru marfa
            voluminoasa recomandam autoutilitara cu prelata, iar pentru vehicule imobilizate sau
            avariate folosim platforma dedicata pentru tractari.
          </p>
          <div className={styles.actionRow}>
            <Link to="/calculator-pret" className={styles.primaryButton}>
              Estimeaza costul
            </Link>
            <Link to="/contact" className={styles.secondaryButton}>
              Verifica disponibilitatea
            </Link>
          </div>
        </article>
      </section>

      <section className={styles.quoteBanner}>
        <div className={styles.quoteText}>
          <h2 className={styles.quoteTitle}>Ai o cursa atipica sau o incarcare speciala?</h2>
          <p>
            Trimite-ne cateva detalii despre volum, traseu si interval. Iti spunem rapid daca se
            potriveste unei configuratii existente sau daca trebuie ajustata planificarea.
          </p>
        </div>
        <div className={styles.quoteActions}>
          <Link to="/contact" className={styles.primaryButton}>
            Discuta cu noi
          </Link>
        </div>
      </section>
    </PresentationLayout>
  );
};

export default Flota;
