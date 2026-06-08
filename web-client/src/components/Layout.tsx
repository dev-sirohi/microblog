import React from 'react';
import Navbar from './Navbar';

interface LayoutProps {
    children: React.ReactNode;
    showNav?: boolean;
}

export default function Layout({ children, showNav = true }: LayoutProps) {
    return (
        <div className="min-h-screen" style={{ backgroundColor: 'var(--color-bg)' }}>
            {showNav && <Navbar />}
            <main className="max-w-2xl mx-auto px-4 py-6">
                {children}
            </main>
        </div>
    );
}
