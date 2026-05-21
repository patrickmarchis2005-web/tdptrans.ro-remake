import { useEffect, useState } from 'react';
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import { contactCards, navItems } from '../data/presentationContent';
import { clearStoredUser, getStoredUser, isAdminUser } from '../utils/session';
import styles from '../pages/styles/Presentation.module.css';

const PresentationLayout = ({ children }) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const currentUser = getStoredUser();
  const adminUser = isAdminUser(currentUser);

  useEffect(() => {
    setIsMenuOpen(false);
  }, [location.pathname]);

  const handleLogout = () => {
    clearStoredUser();
    navigate('/login');
  };

  return (
    <div className={styles.pageFrame}>
      <header className={styles.siteHeader}>
        <div className={styles.headerInner}>
          <Link to="/acasa" className={styles.brand}>
            <span className={styles.brandLabel}>tdptransport.ro</span>
            <span className={styles.brandText}>Transport marfa si tractari auto</span>
          </Link>

          <button
            type="button"
            className={styles.menuToggle}
            aria-expanded={isMenuOpen}
            aria-label="Deschide meniul"
            onClick={() => setIsMenuOpen((currentValue) => !currentValue)}
          >
            Menu
          </button>

          <nav className={styles.primaryNav} data-open={isMenuOpen}>
            <div className={styles.navList}>
              {navItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/acasa'}
                  className={({ isActive }) =>
                    `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </div>

            <div className={styles.accountArea}>
              {currentUser ? (
                <>
                  <div className={styles.userBadge}>
                    <strong>{currentUser.fullName}</strong>
                    <span>{currentUser.roleName}</span>
                  </div>

                  {adminUser && (
                    <Link to="/comenzi" className={styles.accountLink}>
                      Panou admin
                    </Link>
                  )}

                  <button type="button" className={styles.logoutButton} onClick={handleLogout}>
                    Schimba contul
                  </button>
                </>
              ) : (
                <>
                  <Link to="/contact" className={styles.navAction}>
                    Solicita oferta
                  </Link>
                  <Link to="/signup" className={styles.accountLink}>
                    Creeaza cont
                  </Link>
                  <Link to="/login" className={styles.accountLink}>
                    Login
                  </Link>
                </>
              )}
            </div>
          </nav>
        </div>
      </header>

      <main className={styles.pageContent}>{children}</main>

      <footer className={styles.siteFooter}>
        <div className={styles.footerGrid}>
          <div className={styles.footerBlock}>
            <h3>TDP TRANSPORT</h3>
            <p>
              Solutii rapide pentru transport marfa, mutari si tractari auto, cu focus pe claritate,
              disponibilitate si executie sigura.
            </p>
          </div>

          <div className={styles.footerBlock}>
            <h3>Pagini utile</h3>
            <div className={styles.footerLinks}>
              {navItems.map((item) => (
                <Link key={item.to} to={item.to} className={styles.footerLink}>
                  {item.label}
                </Link>
              ))}
            </div>
          </div>

          <div className={styles.footerBlock}>
            <h3>Contact rapid</h3>
            <p>{contactCards[0].value}</p>
            <span className={styles.footerLink}>
              {contactCards[1].value}
            </span>
            <p>{contactCards[2].value}</p>
          </div>
        </div>

        <div className={styles.footerBottom}>
          <span>TDP TRANSPORT: Iti purtam poverile.</span>
          <span>TDP TRANSPORT: We carry your weight.</span>
        </div>
      </footer>
    </div>
  );
};

export default PresentationLayout;
