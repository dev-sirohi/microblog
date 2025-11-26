import React from 'react';
import AuthApi from '../api/AuthApi';
import * as MUI from '@mui/material';
import type { UserProfile, LoginRequest, ApiResponse } from '../interfaces/GlobalInterfaceExport';
import { GlobalDialog } from '../globalDialogRef';
import AppUtils from '../utils/AppUtils';
import { AppConstants } from '../utils/enums';

export default function Login(): React.ReactNode {
    const [pageForm, setPageForm] = React.useState({
        username: "",
        email: "",
        password: "",
    });

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const loginRequest: LoginRequest = {
                username: pageForm.username,
                email: pageForm.email,
                password: pageForm.password,
            };
            const res: ApiResponse<UserProfile> = await AuthApi.loginUser(loginRequest);
            if (AppUtils.isPositiveNumber(res.Data?.id)) {
                // redirect to home page 
            } else {
                // prompt error + Go to signup?
            }
        } catch (ex: any) {
            GlobalDialog.showError(ex.message);
        }
    }

    return (
        <MUI.Container maxWidth="sm">
            <MUI.Box mt={8}>
                <MUI.Typography variant="h4" color="primary">
                    Log In
                </MUI.Typography>

                <MUI.Box component="form" mt={3} onSubmit={handleSubmit}>
                    <MUI.TextField
                        label="Username"
                        fullWidth
                        margin="normal"
                        value={pageForm.username}
                        onChange={(e) => setPageForm({ ...pageForm, username: e.target.value })}
                    />

                    <MUI.TextField
                        label="Email"
                        fullWidth
                        margin="normal"
                        value={pageForm.email}
                        onChange={(e) => setPageForm({ ...pageForm, email: e.target.value })}
                    />

                    <MUI.TextField
                        label="Password"
                        type="password"
                        fullWidth
                        margin="normal"
                        value={pageForm.password}
                        onChange={(e) => setPageForm({ ...pageForm, password: e.target.value })}
                    />

                    <MUI.Button
                        type="submit"
                        variant="contained"
                        color="primary"
                        fullWidth
                        sx={{ mt: 2 }}
                    >
                        Register
                    </MUI.Button>
                </MUI.Box>
            </MUI.Box>
        </MUI.Container>
    );
}