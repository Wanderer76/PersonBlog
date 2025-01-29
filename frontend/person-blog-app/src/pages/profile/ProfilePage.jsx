import API from "../../scripts/apiMethod";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import { BlogPage } from "../blog/BlogPage"
import React, { useState } from "react";

export const ProfilePage = function () {

    //  const [hasBlog, setHasBlog] = useState(false);
    const [profile, setProfile] = useState({
        "id": "",
        "firstName": "",
        "surName": "",
        "email": "",
        "lastName": "",
        "birthdate": null,
        "userId": "",
        "photoUrl": null,
        "profileState": 0,
        "blog": null
    });

    useState(() => {

        var result = API.get("profile/api/Profile/profile", {
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

    return (
        <>
        <h3>Данные профиля</h3>
            <p>ФИО:{`${profile.lastName} ${profile.firstName} ${profile.lastName}`}</p>
            <p>Почта: {profile.email}</p>
            <p>Юзернейм: {profile.login}</p>
            <br/>
            {profile.blog !== null && <BlogPage />}
        </>);

}

export default ProfilePage;