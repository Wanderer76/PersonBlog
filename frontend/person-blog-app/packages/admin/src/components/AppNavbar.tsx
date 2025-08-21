// components/Navbar/Navbar.tsx
import { Navbar, Nav, Container } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';
import { FaBan, FaHome } from 'react-icons/fa';

const AppNavbar = () => {
  return (
    <Navbar bg="dark" variant="dark" expand="lg" className="mb-4">
      <Container fluid>
        <Navbar.Brand href="/">
          <FaBan className="me-2" />
          Админ панель
        </Navbar.Brand>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto">
            <LinkContainer to="/">
              <Nav.Link>
                <FaHome className="me-1" />
                Главная
              </Nav.Link>
            </LinkContainer>
            <LinkContainer to="/ban-requests">
              <Nav.Link>
                <FaBan className="me-1" />
                Жалобы на посты
              </Nav.Link>
            </LinkContainer>
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default AppNavbar;