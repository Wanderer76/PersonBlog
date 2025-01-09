import React, { useEffect, useRef, useState } from "react";
import { JwtTokenService } from "../../scripts/TokenStrorage";


export class ProfilePage extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            blogId: null
        }
    }

    updateId(id) {
        this.setState({ blogId: id })
        console.log("update " + this.state.blogId)
    }

    render() {
        return (
            <>
                <h2>Профиль</h2>
                <CommonProfileData onReceived={(id) => this.updateId(id)} />
                <br />
                {this.state.blogId !== null && <BlogPosts blogId={this.state.blogId} />}
            </>
        );
    }
}



const CommonProfileData = function (props) {
    const hasMounted = useRef(false);

    const [profile, setProfile] = useState({
        id: null,
        name: "",
        description: null,
        createdAt: "",
        photoUrl: null,
        profileId: ""
    })

    useEffect(() => {
        const url = "http://localhost:7892/profile/Blog/detail";
        if (hasMounted.current) return;

        function sendRequest() {
            fetch(url, {
                headers: {
                    'Authorization': JwtTokenService.getFormatedTokenForHeader(),
                    'Content-Type': 'appplication/json'
                },
                method: 'GET'
            }).then(response => {
                if (response.ok) {
                    hasMounted.current = true;
                    return response.json()
                }
                if (response.status === 401) {
                    JwtTokenService.refreshToken();
                    window.location.reload();
                }
            })
                .then(result => {
                    setProfile(result);
                    props.onReceived(result.id);
                }, [])
        }
        sendRequest();
    })

    return (
        <div>
            <ul>
                <img src={profile.url}></img>
                <li>Называние блога: {profile.name}</li>
                <li>Описание: {profile.description}</li>
                <li>Создан: {profile.createdAt}</li>
            </ul>
        </div>
    );

}


const BlogPosts = function (props) {

    const hasMounted = useRef(false);

    useEffect(() => {
        const url = `http://localhost:7892/profile/Blog/posts/list?blogId=${props.blogId}&page=${1}&limit=${100}`;
        if (hasMounted.current) return;

        function sendRequest() {
            fetch(url, {
                headers: {
                    'Authorization': JwtTokenService.getFormatedTokenForHeader(),
                    'Content-Type': 'appplication/json'
                },
                method: 'GET'
            }).then(response => {
                if (response.ok) {
                    hasMounted.current = true;
                    return response.json()
                }
                if (response.status === 401) {
                    JwtTokenService.refreshToken();
                    window.location.reload();
                }
            })
                .then(result => {
                   console.log(result);
                }, [])
        }
        sendRequest();
    })


    return (
        <div>
            <h2>
                {props.blogId}
            </h2>
        </div>
    );
}