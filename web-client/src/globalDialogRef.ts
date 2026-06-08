import type { DialogConfig } from "./utils/GlobalDialogProvider/DialogInterfaces";

export const GlobalDialog = {
    showDialog: async (_config: DialogConfig): Promise<any> => {
        throw new Error("GlobalDialogProvider is not mounted");
    },

    showError: async (_message: string): Promise<void> => {
        throw new Error("GlobalDialogProvider is not mounted");
    },

    showToast: async (_message: string): Promise<void> => {
        throw new Error("GlobalDialogProvider is not mounted");
    }
};
