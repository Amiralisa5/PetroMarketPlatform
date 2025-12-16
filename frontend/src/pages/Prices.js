import React, { useState, useEffect } from 'react';
import axios from 'axios';

const Prices = () => {
  const [commodities, setCommodities] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchCommodities = async () => {
      try {
        const response = await axios.get('/api/Commodities');
        setCommodities(response.data);
      } catch (error) {
        console.error('Error fetching commodities', error);
      } finally {
        setLoading(false);
      }
    };

    fetchCommodities();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <h2>Commodities Prices</h2>
      {commodities.length === 0 ? (
        <p>No commodities available.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Name</th>
              <th>Unit</th>
            </tr>
          </thead>
          <tbody>
            {commodities.map((item) => (
              <tr key={item.commodityId}>
                <td>{item.commodityId}</td>
                <td>{item.name}</td>
                <td>{item.unit}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default Prices;
