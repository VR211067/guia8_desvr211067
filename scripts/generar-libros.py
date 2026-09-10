"""Herramienta local para generar 100 libros ficticios reproducibles."""
import json
import random
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rng = random.Random(10408)
temas = ['El viaje', 'La historia', 'El secreto', 'La memoria', 'El despertar', 'Las voces', 'El jardín', 'La luz', 'Los caminos', 'La leyenda']
lugares = ['del bosque', 'del mar', 'de la ciudad', 'del tiempo', 'de las montañas', 'del desierto', 'de la noche', 'del río', 'de las estrellas', 'del valle']
nombres = ['Ana', 'Luis', 'María', 'Carlos', 'Elena', 'Pedro', 'Sofía', 'Diego', 'Lucía', 'Miguel']
apellidos = ['García', 'López', 'Martínez', 'Hernández', 'Pérez', 'Ramírez', 'Flores', 'Torres', 'Rivera', 'Castro']
books = [dict(Titulo=f'{temas[(i-1)//10]} {lugares[(i-1)%10]}',
              Autor=f'{rng.choice(nombres)} {rng.choice(apellidos)}', AnioPublicacion=rng.randint(1950, 2026))
         for i in range(1, 101)]
target = root / 'LibrosAPI/Data/libros.json'
target.parent.mkdir(exist_ok=True)
target.write_text(json.dumps(books, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'{len(books)} libros ficticios generados.')
