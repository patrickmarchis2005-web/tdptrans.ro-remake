import React from 'react';
import { clearStoredUser } from '../utils/session';

class AppErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      hasError: false,
      errorMessage: '',
    };
  }

  static getDerivedStateFromError(error) {
    return {
      hasError: true,
      errorMessage: error instanceof Error ? error.message : 'A aparut o eroare necunoscuta.',
    };
  }

  componentDidCatch(error, errorInfo) {
    console.error('App runtime error', error, errorInfo);
  }

  handleReset = () => {
    clearStoredUser();
    window.location.href = '/login';
  };

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    return (
      <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px',
        background: 'linear-gradient(160deg, #eef3f8 0%, #dfe8f2 100%)',
      }}>
        <div style={{
          width: 'min(520px, 100%)',
          backgroundColor: '#ffffff',
          borderRadius: '18px',
          padding: '28px',
          boxShadow: '0 18px 50px rgba(20, 38, 63, 0.16)',
        }}>
          <h1 style={{ marginTop: 0 }}>Frontend indisponibil temporar</h1>
          <p style={{ color: '#4f5d6e', lineHeight: 1.6 }}>
            Aplicatia a intampinat o eroare la pornire. Am afisat aceasta pagina in locul ecranului alb
            pentru ca eroarea sa poata fi recuperata fara resetari manuale.
          </p>
          <p style={{ color: '#b42318', fontWeight: 700 }}>
            {this.state.errorMessage}
          </p>
          <button type="button" onClick={this.handleReset}>
            Reincarca si revino la login
          </button>
        </div>
      </div>
    );
  }
}

export default AppErrorBoundary;
