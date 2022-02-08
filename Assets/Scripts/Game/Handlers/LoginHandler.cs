﻿using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_InputField txtUsername;
    [SerializeField] private TMP_InputField txtPassword;

    [SerializeField] private TextMeshProUGUI loginBtnText;
    [SerializeField] private TextMeshProUGUI invalidUsername;
    [SerializeField] private Toggle rememberMeToggle;
    //[SerializeField] private LoadingHandler loading;

    const string AutoLoginFile = "autologin.conf";
    const string TmpAutoLoginFile = "tmpautologin.conf";

    void Start()
    {
        invalidUsername.gameObject.SetActive(false);
        var savedUsername = PlayerPrefs.GetString("LoginUsername", string.Empty);
        var savedPassword = PlayerPrefs.GetString("LoginPassword", string.Empty);
        if (string.IsNullOrEmpty(savedUsername))
        {
            txtUsername.Select();
        }
        else
        {
            txtUsername.text = savedUsername;
            if (!string.IsNullOrEmpty(savedPassword))
            {
                txtPassword.text = savedPassword;
                txtPassword.Select();
                StartCoroutine(DelayedLogin());
                return;
            }
        }

        HandleAutoLogin();
    }

    public async void Login()
    {
        await LoginImplementation();
    }

    public void ClearPassword()
    {
        PlayerPrefs.SetString("LoginUsername", null);
        PlayerPrefs.SetString("LoginPassword", null);
        txtUsername.text = "";
        txtPassword.text = "";
    }

    private async Task LoginImplementation()
    {
        loginBtnText.text = "LOGGING IN...";
        if (txtUsername.text.Length == 0 || txtPassword.text.Length == 0) return;
        if (await gameManager.RavenNestLoginAsync(txtUsername.text, txtPassword.text))
        {
            gameObject.SetActive(false);
            invalidUsername.enabled = false;

            if (rememberMeToggle != null && rememberMeToggle && rememberMeToggle.isOn)
            {
                PlayerPrefs.SetString("LoginUsername", txtUsername.text);
                PlayerPrefs.SetString("LoginPassword", txtPassword.text);
            }
        }
        else
        {
            invalidUsername.gameObject.SetActive(true);
            invalidUsername.enabled = true;
            loginBtnText.text = "LOGIN";
        }
    }

    public void ActivateTempAutoLogin()
    {
        Shinobytes.IO.File.WriteAllLines(TmpAutoLoginFile, new string[] {
            "user=" + txtUsername.text,
            "pass=" + txtPassword.text
        });
    }
    private void HandleAutoLogin()
    {
        if (!Shinobytes.IO.File.Exists(AutoLoginFile) && !Shinobytes.IO.File.Exists(TmpAutoLoginFile))
        {
            return;
        }

        if (Shinobytes.IO.File.Exists(TmpAutoLoginFile))
        {
            AutoLogin(TmpAutoLoginFile);
            Shinobytes.IO.File.Delete(TmpAutoLoginFile);
            return;
        }

        AutoLogin(AutoLoginFile);
    }

    private void AutoLogin(string AutoLoginFile)
    {
        var loginInfo = ParseAutoLoginConfig(Shinobytes.IO.File.ReadAllLines(AutoLoginFile));
        if (string.IsNullOrEmpty(loginInfo.Username) || string.IsNullOrEmpty(loginInfo.Password))
        {
            return;
        }

        txtUsername.text = loginInfo.Username;
        txtPassword.text = loginInfo.Password;
        StartCoroutine(DelayedLogin());
    }

    private IEnumerator DelayedLogin()
    {
        yield return new WaitForSeconds(1f);
        Login();
    }

    private AutoLoginInfo ParseAutoLoginConfig(string[] vs)
    {
        var user = "";
        var pass = "";

        for (var i = 0; i < vs.Length; ++i)
        {
            var line = vs[i]?.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.IndexOf('=') > 0)
            {
                var data = line.Split('=');
                var key = data[0];
                var value = data[1];
                if (key.ToLower().Trim().IndexOf("pass") >= 0)
                {
                    pass = value.Trim();
                }
                else if (key.ToLower().Trim().IndexOf("user") >= 0)
                {
                    user = value.Trim();
                }
            }
            else
            {
                if (i == 0) user = vs[i]?.Trim();
                if (i == 1) pass = vs[i]?.Trim();
            }
        }

        return new AutoLoginInfo { Username = user, Password = pass };
    }


    private struct AutoLoginInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
