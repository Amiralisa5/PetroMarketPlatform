import React, { useState, useEffect } from 'react';
import axios from 'axios';

const Supplies = () => {
  const [supplies, setSupplies] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchSupplies = async () => {
      try {
        const response = await axios.get('/api/FutureSupplies');
        setSupplies(response.data);
      } catch (error) {
        console.error('Error fetching future supplies', error);
      } finally {
        setLoading(false);
      }
    };

    fetchSupplies();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <h2>Future Supplies</h2>
      {supplies.length === 0 ? (
        <p>No future supplies available.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Commodity</th>
              <th>Volume</th>
              <th>Supply Date</th>
            </tr>
          </thead>
          <tbody>
            {supplies.map((supply) => (
              <tr key={supply.futureSupplyId}>
                <td>{supply.futureSupplyId}</td>
                <td>{supply.commodityId}</td>
                <td>{supply.volume}</td>
                <td>{new Date(supply.supplyDate).toLocaleDateString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default Supplies;
