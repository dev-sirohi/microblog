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
            <div className="flex flex-col gap-3 mt-2">
                <input
                    className="border border-gray-300 rounded px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Identifier"
                    name="identifier"
                    required
                />
                <input
                    className="border border-gray-300 rounded px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Password"
                    name="password"
                    type="password"
                    required
                />
            </div>
        ),
        fields: ["identifier", "password"],
        buttons: [{ label: "OK", action: async (params) => callback(params) }],
        closeOnBackdropClick: false,
    };
}
