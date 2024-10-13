import { test, expect } from '@playwright/test';
let date;
test.describe.serial('Rental Component', () => {
    test.beforeEach(async ({ page }) => {
       
        await page.goto('http://localhost:3000/rental'); // Promenite URL prema vašem frontend serveru
    });

    test('should issue a book successfully', async ({ page }) => {
        await page.goto('http://localhost:3000/rental');
    
        // Čekanje da se učitaju opcije u padajućoj listi
        await page.waitForSelector('select[name="members"]');
        await page.waitForSelector('select[name="books"]');
        
        // Odaberi člana
        await page.selectOption('select[name="members"]', { label: 'proba (test@gmail.com)' });
        
        // Odaberi knjigu
        await page.selectOption('select[name="books"]', { label: 'Dekameron' });
        
        // Klikni na dugme za izdavanje
        await page.click('button:text("Rent Book")');
        
        // Proveri da li se prikazuje uspešna poruka
        const successMessage = await page.locator('.Toastify__toast--success').textContent();
        expect(successMessage).toContain('Book rented successfully!');
        
        // Sačuvaj sliku ekrana
        await page.screenshot({ path: 'Slike/book_rented.png' });
    });
    

    test('should return a book successfully', async ({ page }) => {
        await page.goto('http://localhost:3000/rental');
        const today = new Date();
        date=`${today.getMonth() + 1}/${today.getDate()}/${today.getFullYear()}`;
        console.log(date);
        // Čekanje da se učitaju opcije u padajućoj listi
        await page.waitForSelector('select[name="returnRentals"]');
        
        // Console.log svih opcija pre odabira
       
    
        // Odaberi izdanje za vraćanje
        await page.selectOption('select[name="returnRentals"]', { label: `Dekameron - proba (Rented on: ${date})` });
    
        // Klikni na dugme za vraćanje
        await page.click('button:text("Return Book")');
    
        // Proveri da li se prikazuje uspešna poruka
        const successMessage = await page.locator('.Toastify__toast--success').textContent();
        expect(successMessage).toContain('Knjiga je uspešno vraćena');
    
        // Sačuvaj sliku ekrana
        await page.screenshot({ path: 'Slike/book_returned.png' });
    });

    test('should delete a rental successfully', async ({ page }) => {
       
        await page.goto('http://localhost:3000/rental');
        const today = new Date();
        date=`${today.getMonth() + 1}/${today.getDate()}/${today.getFullYear()}`;
        console.log(date);
       
        await page.waitForSelector('select[name="deleteRentals"]');
        
        // Console.log svih opcija pre odabira
       
    
        // Odaberi izdanje za vraćanje
        await page.selectOption('select[name="deleteRentals"]', { label: `Dekameron - proba (Rented on: ${date})` });
    
        // Klikni na dugme za vraćanje
        await page.click('button:text("Delete Rental")');
    
        // Proveri da li se prikazuje uspešna poruka
        const successMessage = await page.locator('.Toastify__toast--success').textContent();
        expect(successMessage).toContain('Rental deleted successfully!');
        // Osveži listu izdanja
        await page.screenshot({ path: 'Slike/delete_rental.png' });
       
    });
});
