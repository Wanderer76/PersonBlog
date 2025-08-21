

const SecondaryButton = function (props) {

    return (
        <>
            < button className="btn btnSecondary" onClick={() => props.handler()}>
                {props.text}
            </button >
        </>)
}

const PrimaryButton = function (props) {

    return (
        <>
            <button className="btn btnPrimary" onClick={() => props.handler()}>
                {props.text}
            </button>
        </>)
}
