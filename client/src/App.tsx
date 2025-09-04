import { useEffect, useState } from "react";

export default function App() {
    const [ok, setOk] = useState("...");

    useEffect(() => {
        fetch("http://localhost:5000/api/health")
            .then(r => r.json())
            .then(d => setOk(d.ok ? "OK" : "FAIL"));
    }, []);

    return (
        <div className="container py-5">
            <h1>Inventory — Hello 👋</h1>
            <p>API health: {ok}</p>
        </div>
    );
}