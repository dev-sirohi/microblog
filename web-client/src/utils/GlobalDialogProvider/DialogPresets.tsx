import { TextField } from "@mui/material";
import type { DialogConfig } from "./DialogInterfaces";

export const ERROR: (message: string) => DialogConfig = (message: string): DialogConfig => {
    return {
        title: "An error occurred",
        message: message || "",
        closeOnBackdropClick: true,
    };
}

export const TOAST: (message: string) => DialogConfig = (message: string): DialogConfig => {
    return {
        message: message || "",
        isToast: true,
        toastDuration: 2500,
    };
}

export const LOGIN: (callback: (params: any) => Promise<any>) => DialogConfig = (callback): DialogConfig => {
    return {
        title: "Login Required",
        message: "Please log in to continue.",
        html: (
            <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
                <TextField
                    label="Identifier"
                    name="identifier"
                    variant="outlined"
                    required
                    fullWidth
                />
                <TextField
                    label="Password"
                    name="password"
                    type="password"
                    variant="outlined"
                    required
                    fullWidth
                />
            </div>
        ),
        fields: ["identifier", "password"],
        buttons: [{ label: "OK", action: async (params) => callback(params) }],
        closeOnBackdropClick: false,
    };
}
