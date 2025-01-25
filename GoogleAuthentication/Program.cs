// NuGet Microsoft.AspNetCore.Authentication.Google
//Aplikace, kter� umo��uje u�ivatel�m p�ihl�sit se pomoc� Google ��tu. Informace o p�ihl�en�m u�ivateli jsou ulo�eny v cookies a pak u� se jen p�i p�ihla�ov�n� kontroluje to cookie.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);


// P�id�n� autentiza�n�ch slu�eb do kontejneru z�vislost�
// Tato ��st konfiguruje, jak se bude v aplikaci zach�zet s autentizac� u�ivatel�.
builder.Services.AddAuthentication(options =>
{
    // Nastaven� v�choz�ho sch�matu pro ov��ov�n� u�ivatele.
    // CookieAuthenticationDefaults.AuthenticationScheme - Znamen�, �e pou��v�me cookies pro ov��en� u�ivatele.
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Nastaven� v�choz�ho sch�matu pro v�zvu k p�ihl�en� (nap�. kdy� u�ivatel nen� p�ihl�en a chce na chr�n�n� zdroj).
    // GoogleDefaults.AuthenticationScheme - Znamen�, �e pokud je pot�eba u�ivatele vyzvat k p�ihl�en�, budeme pou��vat Google.
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;

    // Nastaven� sch�matu, kter� se pou��v� pro p�ihl�en� u�ivatele po �sp�n� autentizaci.
    // CookieAuthenticationDefaults.AuthenticationScheme - Znamen�, �e po �sp�n�m p�ihl�en� ulo��me autentizaci do cookies.
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
// Nastaven� cookies - Ukl�d� informace o autentizaci u�ivatele do cookies
.AddCookie(options =>
{
    // HttpOnly - Cookie nen� p��stupn� z Javascriptu (zvy�uje bezpe�nost).
    options.Cookie.HttpOnly = true;
    // SecurePolicy - Ur�uje, za jak�ch podm�nek se cookie pos�l�.
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // ur�uje, za jak�ch podm�nek prohl�e� po�le cookie serveru (Always - pouze p�es https, None - i p�es http ...)
    // SameSite - Definuje, jestli cookie m��e b�t posl�na s po�adavky mezi r�zn�mi dom�nami.
    options.Cookie.SameSite = SameSiteMode.Lax; // definuje, kdy m��e prohl�e� poslat cookie spolu s po�adavky z jin�ch webov�ch str�nek. (None - v�dy, Strict - pouze p�i po�adavku ze stejn� dom�ny, Lax - n�co mezi, aby fungovalo p�esm�rov�n� od Google)
    // LoginPath - Cesta, kam bude u�ivatel p�esm�rov�n pokud bude neautorizovan�.
    //options.LoginPath = "/login";
})
// Nastaven� autentizace p�es Google.
.AddGoogle(googleOptions =>
{
    // Aby se mi do cooklies ulo�ily i tokeny, kter� mi Google vr�t�
    googleOptions.SaveTokens = true;
    // AccessType - ur�uje, jak� typ p��stupu k dat�m chci. Offline - po�adavek na refresh token, kter� mi umo�n� z�skat nov� access token, kdy� ten star� vypr��.
    googleOptions.AccessType = "offline";
    // Z�skan� z Google Cloud Console -> Credentials.
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    // URL, na kterou Google provede p�esm�rov�n� po p�ihl�en�. Mus� b�t shodn� s t�m, co je v Google Cloud Console -> Credentials.
    googleOptions.CallbackPath = "/signin-google";

    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.email"); // Nav�c po�adavek pro pr�vo p��stup k emailu (standardn�)
    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.profile"); // Nav�c po�adavek pro pr�vo p��stup k profilu (standardn�)

    //Pro nadstandardn� opr�vn�n� mus� b�t aplikace schv�lena googlem, p��padn� pro f�zi v�voje mus� b�t p�id�ny ��ty v google konzoli mezi testovac� ��ty
    googleOptions.Scope.Add("https://www.googleapis.com/auth/calendar.readonly"); // Pro p��stup k ud�lostem kalend��e
    googleOptions.Scope.Add("https://www.googleapis.com/auth/drive.readonly"); //Pro p��stup k Google Disku
});

// P�id�n� autoriza�n�ch slu�eb. Pou��v� se pro ur�en�, jestli m� u�ivatel p��stup k chr�n�n�m zdroj�m.
builder.Services.AddAuthorization();

// --------------------------------------------------------------------------------

// VYTVO�EN� APLIKACE
// --------------------------------------------------------------------------------

var app = builder.Build();

// POZN�MKA: Nejd�le�it�j�� je, aby `UseAuthentication()` bylo vol�no P�ED `UseAuthorization()`!
// Middleware pro autentizaci se spust� jako prvn�, aby mohla identifikovat u�ivatele.
app.UseAuthentication();
// Middleware pro autorizaci je druh�, aby rozhodl, jestli m� u�ivatel p��stup k zdroj�m.
app.UseAuthorization();

// --------------------------------------------------------------------------------
// DEFINICE ENDPOINT�
// --------------------------------------------------------------------------------

// Hlavn� str�nka "/"
// Zde se rozhodne, jestli je u�ivatel p�ihl�en�, a podle toho se zobraz� obsah.
app.MapGet("/", async context =>
{
    // Zkontroluje, jestli je u�ivatel p�ihl�en�.
    if (context.User.Identity?.IsAuthenticated ?? false)
    {
        // Pokud ano, zobraz� jeho jm�no.
        await context.Response.WriteAsync($"Hello, {context.User.Identity.Name}!");
        //context.Response.Redirect("http://www.seznam.cz"); - p��padn� p�esm�ruje na jinou str�nku.
    }
    else
    {
        // Pokud nen�, p�esm�ruje na p�ihla�ovac� str�nku.
        context.Response.Redirect("/login");
    }
});

// P�ihla�ovac� str�nka "/login"
// Vol� Google pro p�ihl�en�.
app.MapGet("/login", async (HttpContext context) =>
{
    // Spust� Google p�ihla�ovac� proces.
    //  AuthenticationProperties  - umo��uje p�idat dopl�uj�c� parametry k autentiza�n�mu procesu.
    //  RedirectUri - Po �sp�n�m p�ihl�en� p�esm�ruje u�ivatele na endpoint /a
    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties: new AuthenticationProperties { RedirectUri = "/a" });
});



/*
Callback endpoint pro Google "/signin-google"
Zpracuje odpov�� od Google po �sp�n�m (i ne�sp�n�m) p�ihl�en�:
Pokud u�ivatel provede p�ihl�en� p�es Google, tak Google po�le ��dost zp�t na callback adresu (/signin-google). Tato ��dost obsahuje autoriza�n� k�d.
V backendu tv� aplikace pak k�d z�sk� a za n�j se u google dot�e� na token (v�etn� p��stupov�ho tokenu) a informace o u�ivateli.
Tato logika je skryta v bal��ku Microsoft.AspNetCore.Authentication.Google. P��m� p��stup na tento endpoint vyvol� v�jimku.
*/
app.MapGet("/signin-google", async context =>
{
    // Pokus� se autentizovat u�ivatele na z�klad� �daj� z Google. Tato metoda zjist�, zda u�ivatel ji� m� v prohl�e�i cookie, kde je ulo�en� informace o autentizaci.
    var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (authenticateResult.Succeeded)
    {
        // Pokud je autentizace �sp�n�, p�ihl�s� u�ivatele (ulo�� informaci o p�ihl�en� do cookie).
        // Pokud by platnost vypr�ela, aplikace bude st�le fungovat a� na to, �e nebude moci z�skat z google informace o u�ivateli.
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal);
        // P�esm�ruje u�ivatele na hlavn� str�nku.

        context.Response.Redirect("/a");
    }
    else
    {
        // Pokud je autentizace ne�sp�n�, p�esm�ruje zp�t na p�ihla�ovac� str�nku.
        context.Response.Redirect("/login");
    }
});

//Zajist� pomoc� refresh tokenu aktualizaci access tokenu v cookies
app.MapGet("/refresh-token", [Authorize] async (HttpContext context) => {
    //Pokus� se autentizovat u�ivatele na z�klad� �daj� ulo�en�ch v cookie. Pokud je u�ivatel p�ihl�en�
    //(m� platnou autentiza�n� cookie), z�sk� z n� informace o u�ivateli. result obsahuje informace o ov��en�,
    //v�etn� Principal (identita u�ivatele) a Properties (vlastnosti autentizace, kam se ukl�daj� tokeny).
    var result = await context.AuthenticateAsync();
    var refreshToken = result.Properties?.GetTokenValue("refresh_token");
    var clientId = builder.Configuration["Authentication:Google:ClientId"];
    var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    // Na konci bloku nebo p�i v�jimce se zavol� Dispose na objektu client.
    using var client = new HttpClient();

    // Vytvo�en� nov�ho po�adavku na z�sk�n� access tokenu pomoc� refresh tokenu a provede zak�dov�n� hodnot (nebezpe�n� znaky)
    // Vytvo�� body post po�adavku ve form�tu x-www-form-urlencoded - to nen� json, proto pak nepou�iji PostAsJsonAsync, ale PostAsync
    var content = new FormUrlEncodedContent(new Dictionary<string, string> {
       { "client_id", clientId },
       { "client_secret", clientSecret },
       { "refresh_token", refreshToken },
       { "grant_type", "refresh_token" }
   });

    var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);

    if (!response.IsSuccessStatusCode)
    {
        // Probl�m p�i z�sk�n� access tokenu. Zkontrolujeme chybu.
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        if (errorResponse != null && errorResponse.TryGetPropertyValue("error", out var error) && error != null && error.ToString() == "invalid_grant")
        {
            // Refresh token je neplatn�, p�esm�ruj na p�ihla�ovac� str�nku
            context.Response.Redirect("/login");
            return "Refresh token invalid";
        }
        else
        {
            //Jin� probl�m p�i z�sk�n� access tokenu - m��eme vr�tit detailn� informace
            return $"Error: {response.StatusCode}, {errorResponse}";
        }
    }

    var tokenResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
    var newAccessToken = tokenResponse["access_token"].ToString();

    // Aktualizace tokenu v cookie
    var props = result.Properties;
    props.UpdateTokenValue("access_token", newAccessToken);
    // Aktualizuje autentiza�n� cookie u�ivatele nov�m access tokenem.
    // Nov� cookie se ulo�� do prohl�e�e a p�i p��t�m po�adavku se pou�ije.
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal, props);

    return newAccessToken;
});

// chr�n�n� endpoint, pokud nejsem p�ihl�en, vyvol� se p�ihl�en� (options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;) a pak dojde k p�esm�rov�n� zp�t sem
app.MapGet("/a", [Authorize] async (HttpContext context) => {
        // Z�sk�n� token� (zde v aplikaci je d�le nevyu��v�m, ale do budoucna bych mohl)
        var result = await context.AuthenticateAsync();
        var refreshToken = result.Properties?.GetTokenValue("refresh_token");
        var accessToken = result.Properties?.GetTokenValue("access_token");
        return $"refresh token: {refreshToken} \r\n access token: {accessToken}";
    });

// --------------------------------------------------------------------------------
// SPU�T�N� APLIKACE
// --------------------------------------------------------------------------------

app.Run();