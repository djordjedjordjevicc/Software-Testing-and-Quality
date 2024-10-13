import { test, expect } from '@playwright/test';
import * as path from 'path';

test.describe.serial('Reservation Page Tests', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('http://localhost:3000/reservation');
    });

    test('should fetch and display books', async ({ page }) => {
        await page.waitForSelector('table');
        
        const bookRows = await page.locator('table tbody tr').count();
        expect(bookRows).toBeGreaterThan(0); // Check if at least one book is displayed

        // Screenshot after verifying book display
        await page.screenshot({ path: path.join('Slike', 'books-display.png') });
    });

   
    test('should add a reservation to the last book successfully', async ({ page }) => {
        // Simulate user ID in localStorage if necessary
        await page.evaluate(() => {
            localStorage.setItem('userID', '2'); // Postavi userID ako je potrebno
        });

        // Čekaj da se tabela učita
        await page.waitForSelector('table');

        // Dohvati poslednji red u tabeli
        const rows = await page.locator('table tbody tr').count();
        const lastRow = page.locator(`table tbody tr:nth-child(${rows})`);

        // Proveri da li je dugme "Reserve" prisutno u poslednjem redu
        const reserveButton = lastRow.locator('button:has-text("Reserve")');
        await expect(reserveButton).toBeVisible();

        // Klikni na dugme "Reserve" za poslednju knjigu
        await reserveButton.click();

        // Simuliraj prompt za datum
        await page.evaluate(() => {
            window.prompt = () => '2024-08-15'; // Postavi datum rezervacije
        });

        // Klikni ponovo na dugme "Reserve" da potvrdiš rezervaciju (ako je potrebno)
        await reserveButton.click();

        // Proveri da li se dugme promenilo u "Reserved"
        await expect(lastRow.locator('button')).toHaveText('Reserved');

        // Screenshot posle dodavanja rezervacije
        await page.screenshot({ path: path.join('Slike', 'reservation-added.png') });
    });
    test('should delete a reservation from the first reserved book', async ({ page }) => {
        // Postavi userID ako je potrebno
        await page.evaluate(() => {
            localStorage.setItem('userID', '2');
        });
    
        // Čekaj da se tabela učita
        await page.waitForSelector('table');
    
        // Dohvati prvu knjigu koja ima dugme "Reserved"
        const rows = await page.locator('table tbody tr').count();
        let reservedButtonLocator;
    
        for (let i = 0; i < rows; i++) {
            const row = page.locator(`table tbody tr:nth-child(${i + 1})`);
            const button = row.locator('button:has-text("Reserved")');
    
            if (await button.isVisible()) {
                reservedButtonLocator = button;
                break;
            }
        }
    
        if (!reservedButtonLocator) {
            throw new Error('No reserved button found. Make sure there is at least one reserved book.');
        }
    
        // Postavi listener za prvi dijalog (potvrda brisanja rezervacije)
        page.once('dialog', async dialog => {
            expect(dialog.message()).toContain('Are you sure you want to delete this reservation?');
            await dialog.accept(); // Klikni na "OK"
        });
    
        // Klikni na dugme "Reserved" za prvu knjigu
        await reservedButtonLocator.click();
    
        // Postavi listener za drugi dijalog (obaveštenje o uspešnom brisanju)
        page.once('dialog', async dialog => {
            expect(dialog.message()).toContain('Reservation deleted successfully');
            await dialog.accept(); // Klikni na "OK"
        });
    
        // Čekaj da se dugme promeni sa "Reserved" na "Reserve"
        await page.waitForSelector('button:has-text("Reserve")', { state: 'visible' });
    
        await page.waitForTimeout(4000);
        await page.screenshot({ path: path.join('Slike', 'reservation-deleted.png') });
    });
});
