// components/Loading/LoadingSpinner.tsx
import { Spinner, Container } from 'react-bootstrap';

interface LoadingSpinnerProps {
  message?: string;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ message = "Загрузка..." }) => {
  return (
    <Container className="d-flex flex-column justify-content-center align-items-center" style={{ minHeight: '50vh' }}>
      <Spinner animation="border" variant="primary" role="status">
        <span className="visually-hidden">Загрузка...</span>
      </Spinner>
      {message && <p className="mt-3 text-muted">{message}</p>}
    </Container>
  );
};

export default LoadingSpinner;