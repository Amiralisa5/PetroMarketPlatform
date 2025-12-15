import React from 'react';
import { Routes, Route, Link } from 'react-router-dom';
import Home from './pages/Home';
import News from './pages/News';
import Prices from './pages/Prices';
import Trades from './pages/Trades';
import Supplies from './pages/Supplies';

function App() {
  return (
    <div>
      <nav>
        <ul>
          <li><Link to="/">Home</Link></li>
          <li><Link to="/news">News</Link></li>
          <li><Link to="/prices">Prices</Link></li>
          <li><Link to="/trades">Trades</Link></li>
          <li><Link to="/supplies">Supplies</Link></li>
        </ul>
      </nav>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/news" element={<News />} />
        <Route path="/prices" element={<Prices />} />
        <Route path="/trades" element={<Trades />} />
        <Route path="/supplies" element={<Supplies />} />
      </Routes>
    </div>
  );
}

export default App;
