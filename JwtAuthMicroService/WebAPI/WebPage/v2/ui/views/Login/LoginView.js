import { BaseView } from '../BaseView.js';
import { LoginTemplate } from './LoginTemplate.js';
import { apiMgr } from '../../../managers/ApiManager.js';
import { tokenMgr } from '../../../managers/TokenManager.js';
import { themeMgr } from '../../../managers/ThemeManager.js';

export class LoginView extends BaseView {
  constructor(container) {
    super(container);
    this.state = {
        theme: themeMgr.current,
        username: 'charlottem',
        password: 'charlottempass',
        isLoading: false,
        error: null
    };
  }

  async performLogin() {
    const { username, password } = this.state;

    this.state.isLoading = true;
    this.state.error = null;
    this.updateView();

    try {
      const res = await apiMgr.tokenApi.post("/login", { username, password });
      const { accessToken, refreshToken } = res.data;

      await tokenMgr.saveTokens(accessToken, refreshToken);

      // Clear loading state once tokens are saved
      this.state.isLoading = false;
      this.updateView();

    } catch (err) {
        this.state.error = err.response?.data?.message || err.message || "Login Failed";
        this.state.isLoading = false;
        this.updateView();
    }
  }

  template() {
    return LoginTemplate(this.state, {
        onLogin: () => this.performLogin(),
        onToggleTheme: (checked) => { this.toggleTheme(checked) },
        onUsernameChange: (val) => { this.state.username = val; },
        onPasswordChange: (val) => { this.state.password = val; },
    });
  }
}