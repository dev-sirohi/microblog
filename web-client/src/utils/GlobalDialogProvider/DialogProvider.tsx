import React from "react";
import type { DialogConfig, DialogContextType, DialogButton } from "./DialogInterfaces";
import { GlobalDialog } from "../../globalDialogRef";
import { ERROR, TOAST } from "./DialogPresets";
import { createPortal } from "react-dom";
import { Snackbar, Alert, Dialog, DialogTitle, DialogContent, DialogActions, Button } from "@mui/material";


const DialogContext = React.createContext<DialogContextType | undefined>(undefined);

export function useGlobalDialog() {
    const context = React.useContext(DialogContext);
    if (!context) {
        throw new Error("useGlobalDialog must be used within a GlobalDialogProvider");
    }
    return context;
}

export function GlobalDialogProvider({ children }: { children: React.ReactNode }) {
    const [dialogConfig, setDialogConfig] = React.useState<DialogConfig | null>(null);
    const [promiseResolver, setPromiseResolver] =
        React.useState<((value: any) => void) | null>(null);

    const toastTimeoutRef = React.useRef<any>(null);

    const showDialog = async (config: DialogConfig): Promise<any> => {
        if (!config.isToast && (config.buttons || []).length === 0) {
            config.buttons = [{ label: "OK" }];
        }
        if (config.closeOnBackdropClick === undefined) {
            config.closeOnBackdropClick = true;
        }

        return new Promise(resolve => {
            setDialogConfig(config);
            setPromiseResolver(() => resolve);

            if (config.isToast) {
                clearTimeout(toastTimeoutRef.current);
                toastTimeoutRef.current = setTimeout(() => {
                    hideDialog(null);
                }, config.toastDuration ?? 2500);
            }
        });
    };

    const hideDialog = (value?: any) => {
        if (promiseResolver) promiseResolver(value);

        setPromiseResolver(null);
        setDialogConfig(null);
    };

    const handleButtonClick = async (btn: DialogButton, config?: DialogConfig) => {
        if (btn.action) {
            let params: any = undefined;

            if (config?.fields && Array.isArray(config.fields)) {
                params = {};
                config.fields.forEach(fieldName => {
                    const elem = document.getElementsByName(fieldName)[0] as HTMLInputElement;
                    if (elem) {
                        params[fieldName] = elem.value;
                    }
                });
            }

            if (btn.value === undefined) {
                // start loading symbol
                const result = await btn.action(params);
                // hide loading symbol
                hideDialog(result);
                return;
            }
            await btn.action(params);
        }

        hideDialog(btn.value);
    };

    const handleBackdropClick = () => {
        if (dialogConfig?.closeOnBackdropClick) hideDialog(null);
    };

    GlobalDialog.showDialog = showDialog;
    GlobalDialog.showError = async (message: string) => {
        return await showDialog(ERROR(message));
    };
    GlobalDialog.showToast = async (message: string) => {
        return await showDialog(TOAST(message));
    };


    return (
        <DialogContext.Provider value={{ showDialog, hideDialog }}>
            {children}

            {createPortal(
                <Snackbar
                    open={!!dialogConfig?.isToast}
                    autoHideDuration={dialogConfig?.toastDuration ?? 2500}
                    onClose={() => hideDialog(null)}
                    anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
                >
                    <Alert severity="info" variant="filled">
                        {dialogConfig?.message}
                    </Alert>
                </Snackbar>,
                document.body
            )}

            {dialogConfig && !dialogConfig.isToast &&
                createPortal(
                    <Dialog
                        open={true}
                        onClose={() => dialogConfig.closeOnBackdropClick && hideDialog(null)}
                    >
                        {dialogConfig.title && <DialogTitle>{dialogConfig.title}</DialogTitle>}

                        <DialogContent>
                            {dialogConfig.message}
                        </DialogContent>

                        <DialogActions>
                            {dialogConfig.buttons?.map((btn, i) => (
                                <Button
                                    key={i}
                                    onClick={() => handleButtonClick(btn, dialogConfig)}
                                    variant="contained"
                                    color="primary"
                                >
                                    {btn.label}
                                </Button>
                            ))}
                        </DialogActions>
                    </Dialog>,
                    document.body
                )
            }
        </DialogContext.Provider>
    );
}

