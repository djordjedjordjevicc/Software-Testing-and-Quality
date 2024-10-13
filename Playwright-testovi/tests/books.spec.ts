import { test, expect } from '@playwright/test';

test.describe.serial('Books Management', () => {
    test('should delete a book successfully', async ({ page }) => {
        await page.goto('http://localhost:3000/books');
        await page.screenshot({ path: 'Slike/before_delete_book.png' });
        const bookRows = page.locator('table tbody tr');
        const lastBookDeleteButton = bookRows.locator('button:has-text("Delete")').last();
        const lastBookRow = await lastBookDeleteButton.locator('..').locator('..');
        const lastBookTitle = await lastBookRow.locator('td:nth-child(1)').textContent();

        await page.screenshot({ path: 'Slike/before_delete_book.png' });

        // Listen for the 'confirm' dialog and accept it
        page.on('dialog', async dialog => {
            await dialog.accept();
        });

        await lastBookDeleteButton.click();
        await page.waitForTimeout(2000); 
        await page.screenshot({ path: 'Slike/after_delete_book.png' });

        
        
    });

    test('should edit a book successfully', async ({ page }) => {
        await page.goto('http://localhost:3000/books');
        
        const bookRows = page.locator('table tbody tr');
        const lastBookEditButton = bookRows.locator('button:has-text("Edit")').last();
        const lastBookRow = await lastBookEditButton.locator('..').locator('..');
        const lastBookTitle = await lastBookRow.locator('td:nth-child(1)').textContent();
        await page.screenshot({ path: 'Slike/before_edit_book.png' });
        await lastBookEditButton.click();

        const newTitle = 'Hamlet';
        await page.fill('input[name="Naslov"]', newTitle);

        await page.click('button:has-text("Save")');
        await page.waitForTimeout(2000); 
        await page.screenshot({ path: 'Slike/after_edit_book.png' });

       
    });
    test('should add a new book successfully', async ({ page }) => {
        await page.goto('http://localhost:3000/books');
       
        const newBook = {
            title: 'Test Book',
            author: 'Test Author',
            isbn: '123456789',
            year: '2024',
            genre: 'Test Genre'
        };
        await page.screenshot({ path: 'Slike/before_add_book.png' });
        // Otvori modal za dodavanje knjige
        await page.click('button:has-text("Add Book")');
    
        // Sačekaj da se modal prikaže
        await page.waitForSelector('.modal');
    
        // Popuni formu
        await page.fill('input[name="Naslov"]', newBook.title);
        await page.fill('input[name="Autor"]', newBook.author);
        await page.fill('input[name="ISBN"]', newBook.isbn);
        await page.fill('input[name="Godina Izdanja"]', newBook.year);
        await page.fill('input[name="Zanr"]', newBook.genre);
    
        // Sačekaj da dugme za dodavanje bude vidljivo i klikni ga
        await page.waitForSelector('button:has-text("Add new book")');
        await page.click('button:has-text("Add new book")');
    
        // Sačekaj da se tabela osveži
        await page.waitForTimeout(1000); 
    
        // Proveri da li se knjiga pojavljuje u tabeli
        const bookRows = page.locator('table tbody tr');
        const bookTitles = await bookRows.locator('td:nth-child(1)').allTextContents();
        expect(bookTitles).toContain(newBook.title);
    
        // Snimak ekrana za proveru
        await page.screenshot({ path: 'Slike/after_add_book.png' });
    });
    
    
    
});
