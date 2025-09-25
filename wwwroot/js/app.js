const API_KEY = "FRONTEND-HARDCODED-123";

async function calculateLoan() {
    const principal = document.getElementById("principal").value;
    const interest = document.getElementById("interest").value;
    const months = document.getElementById("months").value;

    try {
        const response = await fetch(`/api/exchange/usd-to-idr?key=${API_KEY}`);
        const data = await response.json();

        const rate = data.rate ?? 15000;
        const monthlyRate = (interest / 100) / 12;
        const n = months;
        const monthlyPayment = principal *
            (monthlyRate * Math.pow(1 + monthlyRate, n)) /
            (Math.pow(1 + monthlyRate, n) - 1);

        document.getElementById("result").innerHTML = `
            <h4>📊 Hasil Perhitungan</h4>
            <table class="table table-bordered bg-white shadow-sm">
                <tr><th>Principal (USD)</th><td>$${principal}</td></tr>
                <tr><th>Bunga (%)</th><td>${interest}%</td></tr>
                <tr><th>Tenor (Bulan)</th><td>${months}</td></tr>
                <tr class="table-warning"><th>Cicilan / Bulan (USD)</th><td>$${monthlyPayment.toFixed(2)}</td></tr>
                <tr class="table-success"><th>Cicilan / Bulan (IDR)</th><td>Rp ${(monthlyPayment * rate).toFixed(0)}</td></tr>
                <tr><th>Kurs USD → IDR</th><td>${rate}</td></tr>
            </table>
        `;
    } catch (err) {
        document.getElementById("result").innerHTML = `
            <div class="alert alert-danger">Gagal memuat data kurs.</div>
        `;
    }
}