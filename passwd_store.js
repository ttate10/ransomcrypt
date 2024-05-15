import express, { urlencoded } from 'express';
import { appendFile } from 'fs';
import bcrypt from 'bcrypt';
import rateLimit from 'express-rate-limit';
import { config } from 'dotenv';
config();

const app = express();
app.use(urlencoded({ extended: true }));

const limiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100 // limit each IP to 100 requests per windowMs
});

app.use(limiter);

app.post('/submit-key', async (req, res) => {
    const { password } = req.body;
    const salt = await bcrypt.genSalt(10);
    const hashedPassword = await bcrypt.hash(password, salt);
    appendFile('keys.txt', `Hashed: ${hashedPassword}\n`, err => {
        if (err) throw err;
        res.send('Key received');
    });
});

const PORT = process.env.PORT || 55555;

app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});