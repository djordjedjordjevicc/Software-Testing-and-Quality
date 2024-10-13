import { test, expect } from '@playwright/test';

test.describe.serial('Registration Tests', () => {
    test('should register successfully with valid credentials', async ({ page }) => {
        await page.goto('http://localhost:3000/registration');
        
        await page.fill('input[name="username"]', 'pera');
        await page.fill('input[name="password"]', 'pera');
        await page.fill('input[name="name"]', 'Pera');
        await page.fill('input[name="lastName"]', 'Peric');
        await page.fill('input[name="email"]', 'pera@gmail.com');
        
        await page.click('button[type="submit"]');
        
        // Čekanje da se poruka pojavi
        await page.waitForTimeout(2000); // povećano vreme čekanja

        // Proveri da li poruka postoji i sadrži tačan tekst
        const alertMessage = await page.locator('text=Registration successful.').textContent();
        expect(alertMessage).toContain('Registration successful.');

        // Snimi screenshot samo u slučaju uspešnog testa
        await page.screenshot({ path: 'Slike/success_registration.png' });
    });

    test('should show error for existing username', async ({ page }) => {
        await page.goto('http://localhost:3000/registration');
        await page.fill('input[name="username"]', 'djole');
        await page.fill('input[name="password"]', 'djole');
        await page.fill('input[name="name"]', 'Djole');
        await page.fill('input[name="lastName"]', 'Djordjevic');
        await page.fill('input[name="email"]', 'djole@example.com');
        await page.click('button[type="submit"]');
        await page.waitForTimeout(1000);
        const alertMessage = await page.locator('text=Username already exists.').textContent();
        expect(alertMessage).toBe('Username already exists.');
        if (alertMessage == 'Username already exists.') {
            await page.screenshot({ path: 'Slike/failure_existing_username.png' });
        }
    });

    
});
