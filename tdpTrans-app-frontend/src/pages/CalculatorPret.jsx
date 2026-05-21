import { useState } from 'react';
import { Link } from 'react-router-dom';
import PresentationLayout from '../components/PresentationLayout';
import { estimateExamples } from '../data/presentationContent';
import styles from './styles/Presentation.module.css';

const urgencyFees = {
  Planificat: 0,
  Rapid: 90,
  Urgent: 160,
};

const handlingFees = {
  Standard: 0,
  Extra: 80,
  Premium: 150,
};

const CalculatorPret = () => {
  const [formData, setFormData] = useState({
    serviceType: 'Transport',
    distance: 60,
    urgency: 'Planificat',
    handling: 'Standard',
    returnTrip: false,
  });

  const distance = Math.max(0, Number(formData.distance) || 0);
  const basePrice = formData.serviceType === 'Transport' ? 170 : 220;
  const distanceFee = Math.round(distance * (formData.serviceType === 'Transport' ? 3.2 : 4.6));
  const urgencyFee = urgencyFees[formData.urgency];
  const handlingFee = handlingFees[formData.handling];
  const returnFee = formData.returnTrip ? (formData.serviceType === 'Transport' ? 110 : 140) : 0;
  const estimate = basePrice + distanceFee + urgencyFee + handlingFee + returnFee;

  const handleChange = (event) => {
    const { name, type, value, checked } = event.target;
    setFormData((currentValue) => ({
      ...currentValue,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  return (
    <PresentationLayout>
      <section className={styles.pageHero}>
        <span className={styles.pageHeroMeta}>Estimare orientativa</span>
        <h1 className={styles.pageHeroTitle}>Calculator rapid pentru un pret de plecare.</h1>
        <p className={styles.pageHeroText}>
          Rezultatul de mai jos este orientativ si te ajuta sa vezi cum influenteaza costul tipul de
          cursa, distanta si nivelul de urgenta. Oferta finala se confirma dupa verificarea traseului.
        </p>
      </section>

      <section className={styles.calculatorLayout}>
        <article className={styles.estimatorCard}>
          <div className={styles.sectionHeader}>
            <span className={styles.sectionKicker}>Configureaza cursa</span>
            <h2 className={styles.sectionTitle}>Alege scenariul care se apropie cel mai mult de cererea ta.</h2>
          </div>

          <div className={styles.formGrid}>
            <div className={styles.formField}>
              <label htmlFor="serviceType">Tip serviciu</label>
              <select id="serviceType" name="serviceType" value={formData.serviceType} onChange={handleChange}>
                <option value="Transport">Transport marfa</option>
                <option value="Tractare">Tractare auto</option>
              </select>
            </div>

            <div className={styles.formField}>
              <label htmlFor="distance">Distanta estimata (km)</label>
              <input
                id="distance"
                name="distance"
                type="number"
                min="0"
                value={formData.distance}
                onChange={handleChange}
              />
            </div>

            <div className={styles.formField}>
              <label htmlFor="urgency">Urgenta</label>
              <select id="urgency" name="urgency" value={formData.urgency} onChange={handleChange}>
                <option value="Planificat">Planificat</option>
                <option value="Rapid">Rapid</option>
                <option value="Urgent">Urgent</option>
              </select>
            </div>

            <div className={styles.formField}>
              <label htmlFor="handling">Asistenta la incarcare</label>
              <select id="handling" name="handling" value={formData.handling} onChange={handleChange}>
                <option value="Standard">Standard</option>
                <option value="Extra">Extra</option>
                <option value="Premium">Premium</option>
              </select>
            </div>
          </div>

          <label className={styles.toggleRow} htmlFor="returnTrip">
            <span>Include cursa de retur / repozitionare</span>
            <input
              id="returnTrip"
              name="returnTrip"
              type="checkbox"
              checked={formData.returnTrip}
              onChange={handleChange}
            />
          </label>
        </article>

        <aside className={styles.estimateSummary}>
          <span className={styles.sectionKicker}>Rezultat</span>
          <div className={styles.estimateValue}>{estimate} lei</div>
          <p className={styles.estimateCopy}>Estimare orientativa pentru configuratia selectata.</p>

          <div className={styles.estimateBreakdown}>
            <div className={styles.estimateRow}>
              <span>Tarif de baza</span>
              <strong>{basePrice} lei</strong>
            </div>
            <div className={styles.estimateRow}>
              <span>Distanta</span>
              <strong>{distanceFee} lei</strong>
            </div>
            <div className={styles.estimateRow}>
              <span>Urgenta</span>
              <strong>{urgencyFee} lei</strong>
            </div>
            <div className={styles.estimateRow}>
              <span>Asistenta</span>
              <strong>{handlingFee} lei</strong>
            </div>
            <div className={styles.estimateRow}>
              <span>Retur / repozitionare</span>
              <strong>{returnFee} lei</strong>
            </div>
          </div>
        </aside>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionKicker}>Repere rapide</span>
          <h2 className={styles.sectionTitle}>Exemple de plecare pentru cereri uzuale.</h2>
        </div>

        <div className={styles.priceGrid}>
          {estimateExamples.map((example) => (
            <article key={example.label} className={styles.priceCard}>
              <h3 className={styles.cardTitle}>{example.label}</h3>
              <div className={styles.priceValue}>{example.value}</div>
              <p className={styles.cardText}>{example.details}</p>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.featureSplit}>
        <article className={styles.notePanel}>
          <h2 className={styles.panelTitle}>Ce poate modifica pretul final</h2>
          <ul>
            <li>Accesul dificil la incarcare sau descarcare.</li>
            <li>Timpul de asteptare, manipularea suplimentara sau asistenta speciala.</li>
            <li>Programarea in afara intervalului obisnuit sau interventiile foarte urgente.</li>
          </ul>
        </article>

        <article className={styles.notePanel}>
          <h2 className={styles.panelTitle}>Pasul urmator</h2>
          <p className={styles.cardText}>
            Daca estimarea se potriveste cu nevoia ta, continua cu pagina de contact pentru a primi
            confirmarea rutei, a intervalului si a vehiculului disponibil.
          </p>
          <div className={styles.actionRow}>
            <Link to="/contact" className={styles.primaryButton}>
              Solicita oferta finala
            </Link>
            <Link to="/flota" className={styles.secondaryButton}>
              Vezi flota
            </Link>
          </div>
        </article>
      </section>
    </PresentationLayout>
  );
};

export default CalculatorPret;
