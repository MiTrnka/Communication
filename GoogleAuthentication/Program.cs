// NuGet Microsoft.AspNetCore.Authentication.Google
//Aplikace, kter� umo��uje u�ivatel�m p�ihl�sit se pomoc� Google ��tu. Informace o p�ihl�en�m u�ivateli jsou ulo�eny v cookies.
//Aplikace nepou��v� refresh token, tak�e po vypr�en� platnosti tokenu bude u�ivatel p�esm�rov�n na p�ihla�ovac� str�nku.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

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
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Pro localhost je OK None, ale PRO PRODUKCI POU�IJ CookieSecurePolicy.Always.
    // SameSite - Definuje, jestli cookie m��e b�t posl�na s po�adavky mezi r�zn�mi dom�nami.
    options.Cookie.SameSite = SameSiteMode.Lax; // Pro localhost, PRO PRODUKCI m��e b�t pot�eba SameSiteMode.None, pokud je tv�j backend na jin�m subdom�n�, dom�n� ne� frontend.
    // LoginPath - Cesta, kam bude u�ivatel p�esm�rov�n pokud bude neautorizovan�.
    options.LoginPath = "/login";
})
// Nastaven� autentizace p�es Google.
.AddGoogle(googleOptions =>
{
    // Z�skan� z Google Cloud Console -> Credentials.
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    // URL, na kterou se Google vr�t� po p�ihl�en�. Mus� b�t shodn� s t�m, co m� v Google Cloud Console -> Credentials.
    googleOptions.CallbackPath = "/signin-google";

    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.email"); // Pro p��stup k emailu (standardn�)
    googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.profile"); // Pro p��stup k profilu (standardn�)

    //Pro nadstandardn� opr�vn�n� mus� b�t aplikace schv�lena googlem, p��padn� pro f�zi v�voje mus� b�t p�id�ny ��ty v google konzoli mezi testovac� ��ty
    //googleOptions.Scope.Add("https://www.googleapis.com/auth/calendar.readonly"); // Pro p��stup k ud�lostem kalend��e
    //googleOptions.Scope.Add("https://www.googleapis.com/auth/drive.readonly"); //Pro p��stup k Google Disku
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
app.MapGet("/login", async context =>
{
    // Spust� Google p�ihla�ovac� proces.
    //  AuthenticationProperties  - umo��uje p�idat dopl�uj�c� parametry k autentiza�n�mu procesu.
    //  RedirectUri - Po �sp�n�m p�ihl�en� p�esm�ruje u�ivatele zp�t na hlavn� str�nku.
    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties: new AuthenticationProperties { RedirectUri = "/" });
});

/*
Callback endpoint pro Google "/signin-google"
Zpracuje odpov�� od Google po �sp�n�m (i ne�sp�n�m) p�ihl�en�:
Pokud u�ivatel provede p�ihl�en� p�es Google, tak Google po�le ��dost zp�t na callback adresu (/signin-google). Tato ��dost obsahuje autoriza�n� k�d.
V backendu tv� aplikace pak k�d z�sk� a za n�j se u google dot�e� na token (v�etn� p��stupov�ho tokenu) a informace o u�ivateli.
Tato logika se v�ak ned�je p��mo v tv�m k�du (to by nem�lo, bylo by to slo�it�j��), ale je skryta v bal��ku Microsoft.AspNetCore.Authentication.Google.
Jak funguje s Cookies:
Google vr�t� tv�j backend s autoriza�n�m k�dem
Backend pomoc� knihovny Microsoft.AspNetCore.Authentication.Google vym�n� autoriza�n� k�d za data a access token
Backend pak ulo�� do cookies v prohl�e�i informaci, �e se u�ivatel �sp�n� autentizoval.
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
        context.Response.Redirect("/");
    }
    else
    {
        // Pokud je autentizace ne�sp�n�, p�esm�ruje zp�t na p�ihla�ovac� str�nku.
        context.Response.Redirect("/login");
    }
});

// --------------------------------------------------------------------------------
// SPU�T�N� APLIKACE
// --------------------------------------------------------------------------------

app.Run();