export interface DialogButton {
    label: string;
    value?: any;
    action?: (params?: any) => Promise<any> | any;
    className?: string;
}

export interface DialogConfig {
    title?: string;
    message?: string;
    html?: React.ReactNode;
    fields?: string[];
    buttons?: DialogButton[];
    closeOnBackdropClick?: boolean;
    isToast?: boolean;
    toastDuration?: number;
}

export interface DialogContextType {
    showDialog: (config: DialogConfig) => Promise<any>;
    hideDialog: (value?: any) => void;
}