// pages/dashboard/Dashboard.tsx
import { Card, Row, Col, Container } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';
import { FaBan, FaExclamationTriangle, FaUsers } from 'react-icons/fa';

const Dashboard = () => {
  return (
    <Container>
      <h1 className="h2 mb-4">Панель управления</h1>
      
      <Row className="g-4 mb-4">
        <Col md={4}>
          <Card className="h-100 text-center">
            <Card.Body>
              <FaExclamationTriangle className="text-warning mb-3" size={48} />
              <Card.Title>Жалобы на посты</Card.Title>
              <Card.Text>
                Управление жалобами пользователей на контент
              </Card.Text>
              <LinkContainer to="/ban-requests">
                <span className="btn btn-primary">Перейти</span>
              </LinkContainer>
            </Card.Body>
          </Card>
        </Col>
        
        <Col md={4}>
          <Card className="h-100 text-center">
            <Card.Body>
              <FaUsers className="text-info mb-3" size={48} />
              <Card.Title>Управление пользователями</Card.Title>
              <Card.Text>
                Модерация пользователей и управление правами
              </Card.Text>
              <button className="btn btn-primary" disabled>Скоро</button>
            </Card.Body>
          </Card>
        </Col>
        
        <Col md={4}>
          <Card className="h-100 text-center">
            <Card.Body>
              <FaBan className="text-danger mb-3" size={48} />
              <Card.Title>Баны и ограничения</Card.Title>
              <Card.Text>
                Управление блокировками и ограничениями
              </Card.Text>
              <button className="btn btn-primary" disabled>Скоро</button>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default Dashboard;