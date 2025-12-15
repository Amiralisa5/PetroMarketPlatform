import React, { useState, useEffect } from 'react';
import axios from 'axios';

const Trades = () => {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchRequests = async () => {
      try {
        const response = await axios.get('/api/PurchaseRequests');
        setRequests(response.data);
      } catch (error) {
        console.error('Error fetching purchase requests', error);
      } finally {
        setLoading(false);
      }
    };

    fetchRequests();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <h2>Purchase Requests</h2>
      {requests.length === 0 ? (
        <p>No requests found.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Commodity</th>
              <th>Quantity</th>
              <th>Created At</th>
            </tr>
          </thead>
          <tbody>
            {requests.map((req) => (
              <tr key={req.purchaseRequestId}>
                <td>{req.purchaseRequestId}</td>
                <td>{req.commodityId}</td>
                <td>{req.quantity}</td>
                <td>{new Date(req.createdAt).toLocaleDateString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default Trades;
