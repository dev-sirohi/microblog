import React from "react";
import type { DialogConfig, DialogContextType, DialogButton } from "./DialogInterfaces";
import { GlobalDialog } from "../../globalDialogRef";
import { ERROR, TOAST } from "./DialogPresets";
import { createPortal } from "react-dom";

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
                    if (elem) params[fieldName] = elem.value;
                });
            }
            if (btn.value === undefined) {
                const result = await btn.action(params);
                hideDialog(result);
                return;
            }
            await btn.action(params);
        }
        hideDialog(btn.value);
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

            {/* Toast notification */}
            {dialogConfig?.isToast && createPortal(
                <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 animate-fade-in">
                    <div className="bg-gray-800 text-white px-5 py-3 rounded-lg shadow-lg text-sm">
                        {dialogConfig.message}
                    </div>
                </div>,
                document.body
            )}

            {/* Modal dialog */}
            {dialogConfig && !dialogConfig.isToast && createPortal(
                <div
                    className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
                    onClick={() => dialogConfig.closeOnBackdropClick && hideDialog(null)}
                >
                    <div
                        className="bg-white rounded-xl shadow-xl max-w-md w-full mx-4 p-6"
                        onClick={e => e.stopPropagation()}
                    >
                        {dialogConfig.title && (
                            <h2 className="text-lg font-semibold mb-3">{dialogConfig.title}</h2>
                        )}
                        {dialogConfig.message && (
                            <p className="text-sm text-gray-600 mb-4">{dialogConfig.message}</p>
                        )}
                        {dialogConfig.html}
                        <div className="flex justify-end gap-2 mt-4">
                            {dialogConfig.buttons?.map((btn, i) => (
                                <button
                                    key={i}
                                    onClick={() => handleButtonClick(btn, dialogConfig)}
                                    className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
                                >
                                    {btn.label}
                                </button>
                            ))}
                        </div>
                    </div>
                </div>,
                document.body
            )}
        </DialogContext.Provider>
    );
}
