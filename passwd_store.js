import express, { urlencoded } from 'express';
import { appendFileSync } from 'fs';
const app = express();
app.use(urlencoded({ extended: true }));

app.post('/submit-key', (req, res) => {
    const { password, hashedPassword } = req.body;
    // Store the keys securely
    // Example: Append to a file - not recommended for production
    appendFileSync('keys.txt', `Password: ${password}, Hashed: ${hashedPassword}\n`);
    res.send('Key received');
});

app.listen(55555, () => {
    console.log('Server running on port 55555');
});