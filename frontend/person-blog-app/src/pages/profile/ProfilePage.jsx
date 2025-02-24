import API from "../../scripts/apiMethod";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import { BlogPage } from "../blog/BlogPage"
import React, { useState } from "react";
import './ProfilePage.css';

export const ProfilePage = function () {

    const [showModal, setShowModal] = useState(false);
    const [profile, setProfile] = useState({
        id: "",
        firstName: "",
        surName: "",
        email: "",
        lastName: "",
        birthdate: null,
        userId: "",
        photoUrl: null,
        profileState: 0,
        blog: null
    });

    useState(() => {

        API.get("profile/api/Profile/profile", {
            headers: {
                'Authorization': JwtTokenService.getFormatedTokenForHeader()
            }
        }).then(response => {
            if (response.status === 200) {
                console.log(response.data);
                setProfile(response.data);
            }
        })
    }, []);



    function CrateBlogModal(props) {

        const [form, setForm] = useState({
            title: "",
            description: "",
            photoUrl: null
        });
        function updateForm(event) {
            const key = event.target.name;
            const value = (key === 'photoUrl') ? event.target.files[0] : event.target.value;
            setForm((prev) => ({
                ...prev,
                [key]: value
            }))
        }

        async function sendForm(e) {
            const url = "/profile/api/Blog/create";
            let formData = new FormData();
            Object.keys(form).forEach((key) => {
                formData.append(key, form[key])
            });

            await API.post(url, formData, {
                headers: {
                    'Authorization': JwtTokenService.getFormatedTokenForHeader()
                }
            }).then(response => {
                if (response.status === 200) {
                    console.log(response.data)
                }
            })
          
            props.onHandleClose();
            window.location.reload()
        }


        return (<>
            <div className="profile-modal">
                <div className="profile-createPostForm">
                    <p>Создать блог</p>
                    <p>Название</p>
                    <input className="profile-modalContent" placeholder="Название" name="title" onChange={updateForm} />
                    <p>Описание</p>
                    <input className="profile-modalContent" placeholder="Описание" name="description" onChange={updateForm} />
                    <p>Аватарка</p>
                    <input className="profile-modalContent" type="file" accept=".jpg,.jpeg,.png" name="photoUrl" onChange={updateForm} />
                    <br />

                    <button onClick={props.onHandleClose}>Закрыть</button>
                    <button onClick={sendForm}>Создать</button>
                </div>
            </div>
        </>);
    }

    return (
        <>
            <h3>Данные профиля</h3>
            <p>ФИО:{`${profile.lastName} ${profile.firstName} ${profile.lastName}`}</p>
            <p>Почта: {profile.email}</p>
            <p>Юзернейм: {profile.login}</p>
            <br />
            {showModal && <CrateBlogModal onHandleClose={(e) => setShowModal(false)} />}
            {profile.blog !== null && <BlogPage />}
            {profile.blog === null && <button onClick={e => setShowModal(true)}> Создать блог </button>}
        </>);

}




export default ProfilePage;