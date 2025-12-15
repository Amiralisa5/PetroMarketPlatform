import React, { useState, useEffect } from 'react';
import axios from 'axios';

const News = () => {
  const [news, setNews] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchNews = async () => {
      try {
        const response = await axios.get('/api/news');
        setNews(response.data);
      } catch (error) {
        console.error('Error fetching news', error);
      } finally {
        setLoading(false);
      }
    };

    fetchNews();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <h2>News</h2>
      {news.length === 0 ? (
        <p>No news available.</p>
      ) : (
        <ul>
          {news.map((item) => (
            <li key={item.id}>
              <h3>{item.title}</h3>
              <p>{item.summary}</p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default News;
