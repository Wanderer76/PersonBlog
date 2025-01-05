import React from "react";
import styles from './AuthPage.module.css';
import AuthPageForm from "./AuthPageForm";

class AuthPage extends React.Component {
    render() {
        return (
            <div className={styles.background}>
               
                <AuthPageForm />
            </div>)
    }
}

export default AuthPage;