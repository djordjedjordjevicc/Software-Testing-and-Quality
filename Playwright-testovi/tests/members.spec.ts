import { test, expect } from '@playwright/test';

test.describe.serial('Members Management', () => {
    test('should load members list', async ({ page }) => {
        await page.goto('http://localhost:3000/members');

        // Proveri da li je tabela učitana
        const table = await page.locator('table');
        await expect(table).toBeVisible();

        // Proveri da li tabela sadrži očekivane podatke (ovo je primer, prilagodi prema tvojim podacima)
        const rows = await table.locator('tbody tr');
        expect(await rows.count()).toBeGreaterThan(0);

        // Napravi screenshot
        await page.screenshot({ path: 'Slike/members_list_loaded.png' });
    });

    test('should edit a member successfully', async ({ page }) => {
        await page.goto('http://localhost:3000/members');

        const memberRows = page.locator('table tbody tr');
        const editButton = memberRows.locator('button:has-text("Edit")').first();
        await editButton.click();

        await page.screenshot({ path: 'Slike/before_edit_member.png' });

        await page.fill('input[name="Ime"]', 'Test');
        await page.fill('input[name="Prezime"]', 'Testovic');
        await page.fill('input[name="Email"]', 'test@gmail.com');

    
        await page.click('button:has-text("Save")');

        
        await page.screenshot({ path: 'Slike/after_edit_member.png' });

       
        await expect(memberRows.nth(2).locator('td:nth-child(2)')).toHaveText('Test');
        await expect(memberRows.nth(2).locator('td:nth-child(3)')).toHaveText('Testovic');
        await expect(memberRows.nth(2).locator('td:nth-child(4)')).toHaveText('test@gmail.com');
    });
   
 test('should delete a member successfully', async ({ page }) => {
        await page.goto('http://localhost:3000/members');

        const memberRows = page.locator('table tbody tr');
        const lastMemberDeleteButton = memberRows.locator('button:has-text("Delete")').last();
        const lastMemberRow = await lastMemberDeleteButton.locator('..').locator('..');
        const lastMemberUsername = await lastMemberRow.locator('td:nth-child(1)').textContent();

        await page.screenshot({ path: 'Slike/before_delete_member.png' });

        // Listen for the 'confirm' dialog and accept it
        page.on('dialog', async dialog => {
            await dialog.accept();
        });

        await lastMemberDeleteButton.click();

        await page.screenshot({ path: 'Slike/after_delete_member.png' });

        const memberUsernames = await memberRows.locator('td:nth-child(1)').allTextContents();
        expect(memberUsernames).not.toContain(lastMemberUsername);
    });




});
