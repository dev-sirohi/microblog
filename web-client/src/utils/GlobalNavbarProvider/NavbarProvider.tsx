import React from "react";
import { useLocation, useNavigate } from "react-router-dom";

/* Values taken from AI */
const HomeIcon = ({ size = 24 }) => (
    <svg width={size} height={size} fill="none" stroke="black" strokeWidth="2"
        strokeLinecap="round" strokeLinejoin="round" viewBox="0 0 24 24">
        <path d="M3 9l9-7 9 7" />
        <path d="M9 22V12h6v10" />
    </svg>
);

const ProfileIcon = ({ size = 24 }) => (
    <svg width={size} height={size} fill="none" stroke="black" strokeWidth="2"
        strokeLinecap="round" strokeLinejoin="round" viewBox="0 0 24 24">
        <circle cx="12" cy="7" r="4" />
        <path d="M5 21c2-4 6-4 7-4s5 0 7 4" />
    </svg>
);

const PlusIcon = ({ size = 28 }) => (
    <svg width={size} height={size} fill="none" stroke="white" strokeWidth="3"
        strokeLinecap="round" strokeLinejoin="round" viewBox="0 0 24 24">
        <line x1="12" y1="5" x2="12" y2="19" />
        <line x1="5" y1="12" x2="19" y2="12" />
    </svg>
);


export function GlobalNavbarProvider({ children }: { children: React.ReactNode }) {
    const location = useLocation();
    const navigate = useNavigate();

    const hideNavbar = [
        "/login",
        "/register"
    ].some(path => location.pathname === path);

    return (
        <div style={{ width: "100%", height: "100%" }}>
            <div style={{ paddingBottom: hideNavbar ? 0 : 80 }}>
                {children}
            </div>

            {!hideNavbar && (
                <div
                    style={{
                        position: "fixed",
                        bottom: 0,
                        left: 0,
                        width: "100%",
                        height: "65px",
                        background: "#fff",
                        borderTop: "1px solid #ddd",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        padding: "0 35px",
                        zIndex: 9998,
                    }}
                >
                    <button
                        onClick={() => navigate("/")}
                        style={{
                            background: "none",
                            border: "none",
                            paddingLeft: 275,
                            paddingRight: 275,
                            cursor: "pointer",
                        }}
                    >
                        <HomeIcon />
                    </button>

                    <button
                        onClick={() => navigate("/profile")}
                        style={{
                            background: "none",
                            border: "none",
                            paddingLeft: 275,
                            paddingRight: 275,
                            cursor: "pointer",
                        }}
                    >
                        <ProfileIcon />
                    </button>
                </div>
            )}

            {!hideNavbar && (
                <div
                    style={{
                        position: "fixed",
                        bottom: 32,
                        left: "50%",
                        transform: "translateX(-50%)",
                        width: 65,
                        height: 65,
                        background: "black",
                        borderRadius: "50%",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        zIndex: 9999,
                        boxShadow: "0 4px 12px rgba(0,0,0,0.3)",
                        cursor: "pointer",
                    }}
                    onClick={() => navigate("/createpost")}
                >
                    <PlusIcon />
                </div>
            )}
        </div>
    );
}
