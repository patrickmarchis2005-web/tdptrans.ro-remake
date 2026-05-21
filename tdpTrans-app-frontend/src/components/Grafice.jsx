import { memo } from 'react';
import {
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
} from 'recharts';
import styles from '../pages/Comenzi.module.css';

const Grafice = ({ stats }) => {
  const pieData = [
    { name: 'Towing', value: stats?.totalTractari || 0 },
    { name: 'Transport', value: stats?.totalTransportMarfa || 0 },
  ];

  const monthlyData = stats?.monthlyData || [];
  const totalMissions = stats?.totalComenzi ?? stats?.totalCount ?? pieData[0].value + pieData[1].value;
  const colors = ['#49dbdb', '#2a7e7e'];

  return (
    <div className={styles.chartsArea}>
      <header className={styles.chartsHeader}>
        <h2>Statistical View of Orders</h2>
      </header>

      <section className={styles.pieSection}>
        <div className={styles.pieChartWrap}>
          <ResponsiveContainer width="100%" height={240}>
            <PieChart>
              <Pie data={pieData} cx="50%" cy="50%" innerRadius={65} outerRadius={100} dataKey="value">
                {pieData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={colors[index % colors.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className={styles.pieDetails}>
          <h3>Total missions: {totalMissions}</h3>
          <p><span className={styles.colorBoxTowing}></span> Tractare: {pieData[0].value}</p>
          <p><span className={styles.colorBoxTransport}></span> Transport: {pieData[1].value}</p>
        </div>
      </section>

      <section className={styles.barSection} style={{ height: 350, padding: '30px' }}>
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={monthlyData} margin={{ top: 20, right: 24, left: 0, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#edf2f7" />
            <XAxis dataKey="name" stroke="#a0aec0" tick={{ fontSize: 13 }} />
            <YAxis stroke="#a0aec0" allowDecimals={false} />
            <Tooltip cursor={{ fill: '#f4f7f6' }} />
            <Bar dataKey="towing" fill="#49dbdb" radius={[4, 4, 0, 0]} maxBarSize={50} />
            <Bar dataKey="transport" fill="#2a7e7e" radius={[4, 4, 0, 0]} maxBarSize={50} />
          </BarChart>
        </ResponsiveContainer>
      </section>

      <footer className={styles.chartsFooter}>
        <div className={styles.footerLabels}>
          <span className={styles.colorBoxTowingSmall}></span> Tractare
          <span className={styles.colorBoxTransportSmall}></span> Transport
        </div>
        <h3>2026</h3>
      </footer>
    </div>
  );
};

export default memo(Grafice);
