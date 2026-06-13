import psycopg2

try:
    conn = psycopg2.connect(
        host="localhost",
        database="inventory_ac_db",
        user="postgres",
        password="root"
    )
    cur = conn.cursor()
    cur.execute("SELECT id, item_id, action_type FROM stock_transactions WHERE item_id NOT IN (SELECT id FROM items);")
    rows = cur.fetchall()
    print("Orphan transactions:", rows)
    cur.close()
    conn.close()
except Exception as e:
    print("Error:", e)
