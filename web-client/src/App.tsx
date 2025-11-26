import './App.css'
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import {
  Register,
  Login,
  Home
} from './pages/GlobalPagesExport';
import { GlobalNavbarProvider } from './utils/GlobalNavbarProvider/NavbarProvider';
import { GlobalDialogProvider } from './utils/GlobalDialogProvider/DialogProvider';

function App() {
  return (
    <BrowserRouter>
      <GlobalDialogProvider>
        <GlobalNavbarProvider>
          <Routes>
            <Route path="/register" element={<Register />} />
            <Route path="/login" element={<Login />} />
            <Route path="/" element={<Home />} />
          </Routes>
        </GlobalNavbarProvider>
      </GlobalDialogProvider>
    </BrowserRouter>
  );
}

export default App;
