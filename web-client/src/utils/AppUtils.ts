const AppUtils = {
    formatDate(date: string | Date): string {
        const d = new Date(date);
        return d.toLocaleDateString();
    },
    truncate(text: string, maxLength: number): string {
        return text.length <= maxLength ? text : text.slice(0, maxLength) + "...";
    },
    capitalize(str: string): string {
        return str.charAt(0).toUpperCase() + str.slice(1);
    },
    isEmptyOrNull(value: any): boolean {
        return value === null || value === undefined || value === '';
    },
    isEmptyOrWhiteSpace(value: any): boolean {
        return (value === undefined || value === null || (typeof value === "string" && value.trim() === ""));
    },
    isValidEmail(value: string): boolean {
        return !this.isEmptyOrWhiteSpace(value);
    },
    isPositiveNumber(value: any): boolean {
        return !isNaN(value) && Number(value) > 0;
    },
    isEmptyArray(value: any): boolean {
        return Array.isArray(value) && value.filter(x => x !== undefined && x !== null).length > 0;
    }
};

export default AppUtils;