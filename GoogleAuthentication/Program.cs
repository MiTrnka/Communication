// NuGet Microsoft.AspNetCore.Authentication.Google
//Aplikace, která umožòuje uživatelùm pøihlásit se pomocí Google úètu. Informace o pøihlášeném uživateli jsou uloženy v cookies.
//Aplikace nepoužívá refresh token, takže po vypršení platnosti tokenu bude uživatel pøesmìrován na pøihlašovací stránku.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);


// Pøidání autentizaèních služeb do kontejneru závislostí
// Tato èást konfiguruje, jak se bude v aplikaci zacházet s autentizací uživatelù.
builder.Services.AddAuthentication(options =>
{
    // Nastavení výchozího schématu pro ovìøování uživatele.
    // CookieAuthenticationDefaults.AuthenticationScheme - Znamená, že používáme cookies pro ovìøení uživatele.
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Nastavení výchozího schématu pro výzvu k pøihlášení (napø. když uživatel není pøihlášen a chce na chránìný zdroj).
    // GoogleDefaults.AuthenticationScheme - Znamená, že pokud je potøeba uživatele vyzvat k pøihlášení, budeme používat Google.
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;

    // Nastavení schématu, které se používá pro pøihlášení uživatele po úspìšné autentizaci.
    // CookieAuthenticationDefaults.AuthenticationScheme - Znamená, že po úspìšném pøihlášení uložíme autentizaci do cookies.
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
// Nastavení cookies - Ukládá informace o autentizaci uživatele do cookies
.AddCookie(options =>
{
    // HttpOnly - Cookie není pøístupná z Javascriptu (zvyšuje bezpeènost).
    options.Cookie.HttpOnly = true;
    // SecurePolicy - Urèuje, za jakých podmínek se cookie posílá.
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Pro localhost je OK None, ale PRO PRODUKCI POUŽIJ CookieSecurePolicy.Always.
    // SameSite - Definuje, jestli cookie mùže být poslána s požadavky mezi rùznými doménami.
    options.Cookie.SameSite = SameSiteMode.Lax; // Pro localhost, PRO PRODUKCI mùže být potøeba SameSiteMode.None, pokud je tvùj backend na jiném subdoménì, doménì než frontend.
    // LoginPath - Cesta, kam bude uživatel pøesmìrován pokud bude neautorizovaný.
    options.LoginPath = "/login";
})
// Nastavení autentizace pøes Google.
.AddGoogle(googleOptions =>
{
    // Získané z Google Cloud Console -> Credentials.
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    // URL, na kterou se Google vrátí po pøihlášení. Musí být shodná s tím, co máš v Google Cloud Console -> Credentials.
    googleOptions.CallbackPath = "/signin-google";

    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.email"); // Pro pøístup k emailu (standardní)
    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.profile"); // Pro pøístup k profilu (standardní)

    //Pro nadstandardní oprávnìní musí být aplikace schválena googlem, pøípadnì pro fázi vývoje musí být pøidány úèty v google konzoli mezi testovací úèty
    //googleOptions.Scope.Add("https://www.googleapis.com/auth/calendar.readonly"); // Pro pøístup k událostem kalendáøe
    //googleOptions.Scope.Add("https://www.googleapis.com/auth/drive.readonly"); //Pro pøístup k Google Disku
});

// Pøidání autorizaèních služeb. Používá se pro urèení, jestli má uživatel pøístup k chránìným zdrojùm.
builder.Services.AddAuthorization();

// --------------------------------------------------------------------------------

// VYTVOØENÍ APLIKACE
// --------------------------------------------------------------------------------

var app = builder.Build();

// POZNÁMKA: Nejdùležitìjší je, aby `UseAuthentication()` bylo voláno PØED `UseAuthorization()`!
// Middleware pro autentizaci se spustí jako první, aby mohla identifikovat uživatele.
app.UseAuthentication();
// Middleware pro autorizaci je druhý, aby rozhodl, jestli má uživatel pøístup k zdrojùm.
app.UseAuthorization();

// --------------------------------------------------------------------------------
// DEFINICE ENDPOINTÙ
// --------------------------------------------------------------------------------

// Hlavní stránka "/"
// Zde se rozhodne, jestli je uživatel pøihlášený, a podle toho se zobrazí obsah.
app.MapGet("/", async context =>
{
    // Zkontroluje, jestli je uživatel pøihlášený.
    if (context.User.Identity?.IsAuthenticated ?? false)
    {
        // Pokud ano, zobrazí jeho jméno.
        await context.Response.WriteAsync($"Hello, {context.User.Identity.Name}!");
        //context.Response.Redirect("http://www.seznam.cz"); - pøípadnì pøesmìruje na jinou stránku.
    }
    else
    {
        // Pokud není, pøesmìruje na pøihlašovací stránku.
        context.Response.Redirect("/login");
    }
});

// Pøihlašovací stránka "/login"
// Volá Google pro pøihlášení.
app.MapGet("/login", async context =>
{
    // Spustí Google pøihlašovací proces.
    //  AuthenticationProperties  - umožòuje pøidat doplòující parametry k autentizaènímu procesu.
    //  RedirectUri - Po úspìšném pøihlášení pøesmìruje uživatele zpìt na hlavní stránku.
    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties: new AuthenticationProperties { RedirectUri = "/" });
});

/*
Callback endpoint pro Google "/signin-google"
Zpracuje odpovìï od Google po úspìšném (i neúspìšném) pøihlášení:
Pokud uživatel provede pøihlášení pøes Google, tak Google pošle žádost zpìt na callback adresu (/signin-google). Tato žádost obsahuje autorizaèní kód.
V backendu tvé aplikace pak kód získáš a za nìj se u google dotážeš na token (vèetnì pøístupového tokenu) a informace o uživateli.
Tato logika se však nedìje pøímo v tvém kódu (to by nemìlo, bylo by to složitìjší), ale je skryta v balíèku Microsoft.AspNetCore.Authentication.Google.
Jak funguje s Cookies:
Google vrátí tvùj backend s autorizaèním kódem
Backend pomocí knihovny Microsoft.AspNetCore.Authentication.Google vymìní autorizaèní kód za data a access token
Backend pak uloží do cookies v prohlížeèi informaci, že se uživatel úspìšnì autentizoval.
*/
app.MapGet("/signin-google", async context =>
{
    // Pokusí se autentizovat uživatele na základì údajù z Google. Tato metoda zjistí, zda uživatel již má v prohlížeèi cookie, kde je uložená informace o autentizaci.
    var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (authenticateResult.Succeeded)
    {
        // Pokud je autentizace úspìšná, pøihlásí uživatele (uloží informaci o pøihlášení do cookie).
        // Pokud by platnost vypršela, aplikace bude stále fungovat až na to, že nebude moci získat z google informace o uživateli.
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal);
        // Pøesmìruje uživatele na hlavní stránku.
        context.Response.Redirect("/");
    }
    else
    {
        // Pokud je autentizace neúspìšná, pøesmìruje zpìt na pøihlašovací stránku.
        context.Response.Redirect("/login");
    }
});

// --------------------------------------------------------------------------------
// SPUŠTÌNÍ APLIKACE
// --------------------------------------------------------------------------------

app.Run();