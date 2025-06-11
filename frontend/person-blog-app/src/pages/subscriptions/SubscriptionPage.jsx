import { useEffect } from "react";
import API from "../../scripts/apiMethod";


const SubscriptionPage = function () {

    useEffect(() => {

API.get('video/api/Subscriber/subscriptions?page=1&size=20')


    }, [])

    return (
        <>
        </>
    )
}


export default SubscriptionPage;