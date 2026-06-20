import { Archive, Check, Download, Edit3, LogOut, Plus, RotateCcw, Trash2, Upload, X } from 'lucide-react';
import React, { useEffect, useMemo, useState } from 'react';
import { apiRequest } from './api.js';

const blankForm = { courseName: '', description: '', file: null };

function getStoredUser() {
  try {
    return JSON.parse(localStorage.getItem('user') ?? 'null');
  } catch {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    return null;
  }
}

function App() {
  const [authMode, setAuthMode] = useState('login');
  const [authForm, setAuthForm] = useState({ fullName: '', email: 'demo@tetacode.com', password: 'Demo123!' });
  const [user, setUser] = useState(getStoredUser);
  const [notes, setNotes] = useState([]);
  const [archive, setArchive] = useState([]);
  const [tab, setTab] = useState('notes');
  const [form, setForm] = useState(blankForm);
  const [editing, setEditing] = useState(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');

  const visibleNotes = tab === 'notes' ? notes : archive;
  const stats = useMemo(() => ({
    active: notes.length,
    archived: archive.length,
    files: [...notes, ...archive].filter((note) => note.fileUrl).length,
  }), [notes, archive]);

  useEffect(() => {
    if (user) {
      refresh().catch((error) => setMessage(error.message));
    }
  }, [user]);

  async function refresh() {
    const [activeNotes, archivedNotes] = await Promise.all([
      apiRequest('/api/notes'),
      apiRequest('/api/notes/archive'),
    ]);
    setNotes(activeNotes);
    setArchive(archivedNotes);
  }

  async function submitAuth(event) {
    event.preventDefault();
    setBusy(true);
    setMessage('');

    try {
      const payload = authMode === 'login'
        ? { email: authForm.email, password: authForm.password }
        : authForm;
      const result = await apiRequest(`/api/auth/${authMode}`, {
        method: 'POST',
        body: payload,
      });

      localStorage.setItem('token', result.token);
      localStorage.setItem('user', JSON.stringify({ fullName: result.fullName, email: result.email }));
      setUser({ fullName: result.fullName, email: result.email });
    } catch (error) {
      setMessage(error.message);
    } finally {
      setBusy(false);
    }
  }

  async function saveNote(event) {
    event.preventDefault();
    setBusy(true);
    setMessage('');

    const data = new FormData();
    data.append('courseName', form.courseName);
    data.append('description', form.description);
    if (form.file) {
      data.append('file', form.file);
    }

    try {
      await apiRequest(editing ? `/api/notes/${editing.id}` : '/api/notes', {
        method: editing ? 'PUT' : 'POST',
        body: data,
      });
      setForm(blankForm);
      setEditing(null);
      await refresh();
      setMessage(editing ? 'Not guncellendi.' : 'Yeni not eklendi.');
    } catch (error) {
      setMessage(error.message);
    } finally {
      setBusy(false);
    }
  }

  function startEdit(note) {
    setEditing(note);
    setForm({ courseName: note.courseName, description: note.description, file: null });
    setTab('notes');
  }

  async function runAction(action) {
    setBusy(true);
    setMessage('');
    try {
      await action();
      await refresh();
    } catch (error) {
      setMessage(error.message);
    } finally {
      setBusy(false);
    }
  }

  function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
    setNotes([]);
    setArchive([]);
  }

  if (!user) {
    return (
      <main className="auth-page">
        <section className="auth-panel">
          <div>
            <p className="eyebrow">TetaCode Yazilim</p>
            <h1>Ders Notlari Yonetim Sistemi</h1>
            <p className="lead">Kendi hesabinizla giris yapin, notlarinizi yonetin, dosya ekleyin ve arsivden kalici silme akisini kullanin.</p>
          </div>

          <form onSubmit={submitAuth} className="auth-form">
            <div className="segmented">
              <button type="button" className={authMode === 'login' ? 'selected' : ''} onClick={() => setAuthMode('login')}>Giris</button>
              <button type="button" className={authMode === 'register' ? 'selected' : ''} onClick={() => setAuthMode('register')}>Kayit</button>
            </div>

            {authMode === 'register' && (
              <label>
                Ad Soyad
                <input value={authForm.fullName} onChange={(event) => setAuthForm({ ...authForm, fullName: event.target.value })} required />
              </label>
            )}

            <label>
              E-posta
              <input type="email" value={authForm.email} onChange={(event) => setAuthForm({ ...authForm, email: event.target.value })} required />
            </label>

            <label>
              Sifre
              <input type="password" value={authForm.password} onChange={(event) => setAuthForm({ ...authForm, password: event.target.value })} required />
            </label>

            {message && <p className="message error">{message}</p>}
            <button className="primary" disabled={busy}>{busy ? 'Bekleyin...' : authMode === 'login' ? 'Giris yap' : 'Hesap olustur'}</button>
          </form>
        </section>
      </main>
    );
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Ders notlari</p>
          <h1>Not paneli</h1>
        </div>
        <div className="user-menu">
          <span>{user.fullName}</span>
          <button className="icon-button" onClick={logout} title="Cikis yap"><LogOut size={18} /></button>
        </div>
      </header>

      <section className="metrics">
        <div><strong>{stats.active}</strong><span>Aktif not</span></div>
        <div><strong>{stats.archived}</strong><span>Arsiv</span></div>
        <div><strong>{stats.files}</strong><span>Dosyali not</span></div>
      </section>

      <section className="workspace">
        <form className="note-form" onSubmit={saveNote}>
          <div className="form-title">
            <h2>{editing ? 'Notu guncelle' : 'Yeni not ekle'}</h2>
            {editing && <button type="button" className="icon-button" title="Duzenlemeyi iptal et" onClick={() => { setEditing(null); setForm(blankForm); }}><X size={18} /></button>}
          </div>

          <label>
            Ders adi
            <input value={form.courseName} onChange={(event) => setForm({ ...form, courseName: event.target.value })} required />
          </label>

          <label>
            Aciklama
            <textarea value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} required rows="7" />
          </label>

          <label className="file-picker">
            <Upload size={18} />
            <span>{form.file ? form.file.name : editing?.fileName ?? 'PDF, Word veya gorsel dosyasi sec'}</span>
            <input type="file" onChange={(event) => setForm({ ...form, file: event.target.files?.[0] ?? null })} />
          </label>

          <button className="primary" disabled={busy}>
            {editing ? <Check size={18} /> : <Plus size={18} />}
            {editing ? 'Guncelle' : 'Ekle'}
          </button>
        </form>

        <section className="notes-panel">
          <div className="panel-header">
            <div className="segmented">
              <button className={tab === 'notes' ? 'selected' : ''} onClick={() => setTab('notes')}>Aktif</button>
              <button className={tab === 'archive' ? 'selected' : ''} onClick={() => setTab('archive')}>Arsiv</button>
            </div>
            {message && <p className="message">{message}</p>}
          </div>

          <div className="note-list">
            {visibleNotes.length === 0 && <p className="empty">Bu bolumde henuz not yok.</p>}
            {visibleNotes.map((note) => (
              <article className="note-card" key={note.id}>
                <div>
                  <h3>{note.courseName}</h3>
                  <p>{note.description}</p>
                </div>
                <div className="note-meta">
                  <span>Guncelleme: {new Date(note.updatedAt).toLocaleDateString('tr-TR')}</span>
                  {note.fileUrl && <a href={note.fileUrl} target="_blank" rel="noreferrer"><Download size={16} /> {note.fileName}</a>}
                </div>
                <div className="actions">
                  {tab === 'notes' ? (
                    <>
                      <button onClick={() => startEdit(note)}><Edit3 size={16} /> Duzenle</button>
                      <button onClick={() => runAction(() => apiRequest(`/api/notes/${note.id}`, { method: 'DELETE' }))}><Archive size={16} /> Arsivle</button>
                    </>
                  ) : (
                    <>
                      <button onClick={() => runAction(() => apiRequest(`/api/notes/${note.id}/restore`, { method: 'POST' }))}><RotateCcw size={16} /> Geri al</button>
                      <button className="danger" onClick={() => runAction(() => apiRequest(`/api/notes/${note.id}/hard`, { method: 'DELETE' }))}><Trash2 size={16} /> Kalici sil</button>
                    </>
                  )}
                </div>
              </article>
            ))}
          </div>
        </section>
      </section>
    </main>
  );
}

export default App;
