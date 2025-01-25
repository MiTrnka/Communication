// NuGet Microsoft.AspNetCore.Authentication.Google
//Aplikace, která umožòuje uživatelùm pøihlásit se pomocí Google úètu. Informace o pøihlášeném uživateli jsou uloženy v cookies a pak už se jen pøi pøihlašování kontroluje to cookie.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;

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
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // urèuje, za jakých podmínek prohlížeè pošle cookie serveru (Always - pouze pøes https, None - i pøes http ...)
    // SameSite - Definuje, jestli cookie mùže být poslána s požadavky mezi rùznými doménami.
    options.Cookie.SameSite = SameSiteMode.Lax; // definuje, kdy mùže prohlížeè poslat cookie spolu s požadavky z jiných webových stránek. (None - vždy, Strict - pouze pøi požadavku ze stejné domény, Lax - nìco mezi, aby fungovalo pøesmìrování od Google)
    // LoginPath - Cesta, kam bude uživatel pøesmìrován pokud bude neautorizovaný.
    //options.LoginPath = "/login";
})
// Nastavení autentizace pøes Google.
.AddGoogle(googleOptions =>
{
    // Aby se mi do cooklies uložily i tokeny, které mi Google vrátí
    googleOptions.SaveTokens = true;
    // AccessType - urèuje, jaký typ pøístupu k datùm chci. Offline - požadavek na refresh token, který mi umožní získat nový access token, když ten starý vyprší.
    googleOptions.AccessType = "offline";
    // Získané z Google Cloud Console -> Credentials.
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    // URL, na kterou Google provede pøesmìrování po pøihlášení. Musí být shodná s tím, co je v Google Cloud Console -> Credentials.
    googleOptions.CallbackPath = "/signin-google";

    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.email"); // Navíc požadavek pro právo pøístup k emailu (standardní)
    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.profile"); // Navíc požadavek pro právo pøístup k profilu (standardní)

    //Pro nadstandardní oprávnìní musí být aplikace schválena googlem, pøípadnì pro fázi vývoje musí být pøidány úèty v google konzoli mezi testovací úèty
    googleOptions.Scope.Add("https://www.googleapis.com/auth/calendar.readonly"); // Pro pøístup k událostem kalendáøe
    googleOptions.Scope.Add("https://www.googleapis.com/auth/drive.readonly"); //Pro pøístup k Google Disku
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
app.MapGet("/login", async (HttpContext context) =>
{
    // Spustí Google pøihlašovací proces.
    //  AuthenticationProperties  - umožòuje pøidat doplòující parametry k autentizaènímu procesu.
    //  RedirectUri - Po úspìšném pøihlášení pøesmìruje uživatele na endpoint /a
    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties: new AuthenticationProperties { RedirectUri = "/a" });
});



/*
Callback endpoint pro Google "/signin-google"
Zpracuje odpovìï od Google po úspìšném (i neúspìšném) pøihlášení:
Pokud uživatel provede pøihlášení pøes Google, tak Google pošle žádost zpìt na callback adresu (/signin-google). Tato žádost obsahuje autorizaèní kód.
V backendu tvé aplikace pak kód získáš a za nìj se u google dotážeš na token (vèetnì pøístupového tokenu) a informace o uživateli.
Tato logika je skryta v balíèku Microsoft.AspNetCore.Authentication.Google. Pøímý pøístup na tento endpoint vyvolá výjimku.
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

        context.Response.Redirect("/a");
    }
    else
    {
        // Pokud je autentizace neúspìšná, pøesmìruje zpìt na pøihlašovací stránku.
        context.Response.Redirect("/login");
    }
});

//Zajistí pomocí refresh tokenu aktualizaci access tokenu v cookies
app.MapGet("/refresh-token", [Authorize] async (HttpContext context) => {
    //Pokusí se autentizovat uživatele na základì údajù uložených v cookie. Pokud je uživatel pøihlášený
    //(má platnou autentizaèní cookie), získá z ní informace o uživateli. result obsahuje informace o ovìøení,
    //vèetnì Principal (identita uživatele) a Properties (vlastnosti autentizace, kam se ukládají tokeny).
    var result = await context.AuthenticateAsync();
    var refreshToken = result.Properties?.GetTokenValue("refresh_token");
    var clientId = builder.Configuration["Authentication:Google:ClientId"];
    var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    // Na konci bloku nebo pøi výjimce se zavolá Dispose na objektu client.
    using var client = new HttpClient();

    // Vytvoøení nového požadavku na získání access tokenu pomocí refresh tokenu a provede zakódování hodnot (nebezpeèné znaky)
    // Vytvoøí body post požadavku ve formátu x-www-form-urlencoded - to není json, proto pak nepoužiji PostAsJsonAsync, ale PostAsync
    var content = new FormUrlEncodedContent(new Dictionary<string, string> {
       { "client_id", clientId },
       { "client_secret", clientSecret },
       { "refresh_token", refreshToken },
       { "grant_type", "refresh_token" }
   });

    var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);

    if (!response.IsSuccessStatusCode)
    {
        // Problém pøi získání access tokenu. Zkontrolujeme chybu.
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        if (errorResponse != null && errorResponse.TryGetPropertyValue("error", out var error) && error != null && error.ToString() == "invalid_grant")
        {
            // Refresh token je neplatný, pøesmìruj na pøihlašovací stránku
            context.Response.Redirect("/login");
            return "Refresh token invalid";
        }
        else
        {
            //Jiný problém pøi získání access tokenu - mùžeme vrátit detailní informace
            return $"Error: {response.StatusCode}, {errorResponse}";
        }
    }

    var tokenResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
    var newAccessToken = tokenResponse["access_token"].ToString();

    // Aktualizace tokenu v cookie
    var props = result.Properties;
    props.UpdateTokenValue("access_token", newAccessToken);
    // Aktualizuje autentizaèní cookie uživatele novým access tokenem.
    // Nová cookie se uloží do prohlížeèe a pøi pøíštím požadavku se použije.
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal, props);

    return newAccessToken;
});

// chránìný endpoint, pokud nejsem pøihlášen, vyvolá se pøihlášení (options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;) a pak dojde k pøesmìrování zpìt sem
app.MapGet("/a", [Authorize] async (HttpContext context) => {
        // Získání tokenù (zde v aplikaci je dále nevyužívám, ale do budoucna bych mohl)
        var result = await context.AuthenticateAsync();
        var refreshToken = result.Properties?.GetTokenValue("refresh_token");
        var accessToken = result.Properties?.GetTokenValue("access_token");
        return $"refresh token: {refreshToken} \r\n access token: {accessToken}";
    });

// --------------------------------------------------------------------------------
// SPUŠTÌNÍ APLIKACE
// --------------------------------------------------------------------------------

app.Run();