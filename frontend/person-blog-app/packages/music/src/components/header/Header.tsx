import { MusicNote } from "@mui/icons-material";
import { AppBar, Button, TextField, Toolbar, Typography } from "@mui/material";
import { JwtTokenService } from "../../scripts/TokenStrorage";
import { AuthPageUrl, BlogPageUrl } from "../../scripts/apiMethod";


const Header: React.FC = function () {
    return (
        <>
            <AppBar
                position="static"
                sx={{
                    backgroundColor: 'background.paper',
                    color: 'text.primary',
                    boxShadow: 1,
                }}
            >
                <Toolbar>
                    <MusicNote sx={{ color: '#ff7b00', mr: 2 }} />
                    <Typography
                        variant="h6"
                        noWrap
                        component="div"
                        sx={{
                            flexGrow: 1,
                            display: { xs: 'none', sm: 'block' },
                            fontWeight: 'bold',
                        }}
                    >
                        MusicStream
                    </Typography>

                    {!JwtTokenService.isAuth() &&
                        <Button onClick={() => {
                            const loginUrl = new URL(AuthPageUrl);
                            const redirectUrl = window.location.href
                            loginUrl.searchParams.append('redirect', redirectUrl);
                            window.location.href = loginUrl.toString()
                        }}>
                            Вход
                        </Button>
                    }
                    {JwtTokenService.isAuth() &&
                        <Button onClick={() => {
                            const loginUrl = new URL(BlogPageUrl);
                            window.location.href = loginUrl.toString()
                        }}>
                            Перейти в блог
                        </Button>
                    }


                </Toolbar>
            </AppBar>
        </>
    )
}

export default Header;