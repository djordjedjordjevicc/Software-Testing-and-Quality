import { test, expect } from '@playwright/test';
test.describe.serial('Login Tests', () => {
    test('should login successfully with correct credentials', async ({ page }) => {
        // Navigiramo do stranice za login
        await page.goto('http://localhost:3000/login');

        // Popunjavamo formu za login
        await page.fill('input[type="text"]', 'djole'); // Koristite odgovarajuće korisničko ime
        await page.fill('input[type="password"]', 'djole'); // Koristite odgovarajuću lozinku

        // Klik na dugme za login
        await page.click('button[type="submit"]');

        // Provera da li je korisnik uspešno logovan
        await page.waitForSelector('text=Welcome,'); // Proverava da li se pojavljuje poruka "Welcome,"
        const welcomeMessage = await page.textContent('text=Welcome,');
        expect(welcomeMessage).toContain('Welcome,');
        await page.screenshot({ path: 'Slike/success_login.png' }); // Proverava da li poruka sadrži "Welcome,"
    });

    test('should show error with incorrect credentials', async ({ page }) => {
        // Navigiramo do stranice za login
        await page.goto('http://localhost:3000/login');

        // Popunjavamo formu za login sa pogrešnim podacima
        await page.fill('input[type="text"]', 'wrong');
        await page.fill('input[type="password"]', 'wrong');

        // Klik na dugme za login
        await page.click('button[type="submit"]');

        // Provera da li se pojavljuje poruka o grešci
        await page.waitForSelector('text=Invalid username or password.'); // Proverava da li se pojavljuje poruka o grešci
        const errorMessage = await page.textContent('text=Invalid username or password.');
        expect(errorMessage).toBe('Invalid username or password.');
        await page.screenshot({ path: 'Slike/failed_login.png' }); // Proverava da li poruka o grešci sadrži tačan tekst
    });
});
