document.addEventListener('DOMContentLoaded', function(){
  const yearEl = document.getElementById('year');
  if(yearEl) yearEl.textContent = new Date().getFullYear();
});

// Simple auth handling and client-side routing for the static frontend
(function(){
  const authPrimaryBtn = document.getElementById('auth-primary');
  const authSecondaryLink = document.getElementById('auth-secondary');
  const userInfoEl = document.getElementById('user-info');

  function decodeToken(token){
    try{
      const parts = token.split('.');
      if(parts.length !== 3) return null;
      const decoded = JSON.parse(atob(parts[1]));
      return decoded;
    }catch(e){
      return null;
    }
  }

  function isAuthenticated(){
    return !!localStorage.getItem('jwt');
  }

  function setToken(token){
    if(token) localStorage.setItem('jwt', token);
    updateAuthUI();
  }

  function clearToken(){
    localStorage.removeItem('jwt');
    updateAuthUI();
  }

  function updateAuthUI(){
    if(!authPrimaryBtn || !authSecondaryLink) return;
    if(isAuthenticated()){
      const token = localStorage.getItem('jwt');
      const decoded = decodeToken(token);
      const role = decoded ? decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] : 'User';
      if(userInfoEl){
        userInfoEl.textContent = `Role: ${role}`;
        userInfoEl.style.display = '';
      }
      authPrimaryBtn.textContent = 'Logout';
      authPrimaryBtn.onclick = () => { clearToken(); navigateTo('home'); };
      authSecondaryLink.style.display = 'none';
    } else {
      if(userInfoEl) userInfoEl.style.display = 'none';
      authPrimaryBtn.textContent = 'Login';
      authPrimaryBtn.onclick = () => { navigateTo('login'); };
      authSecondaryLink.style.display = '';
      authSecondaryLink.setAttribute('href', '#register');
    }
  }

  // Simple hash router: shows sections by id matching hash (without '#')
  function navigateTo(section){
    if(!section) section = 'home';
    location.hash = `#${section}`;
    showSection(section);
  }

  function showSection(section){
    const sections = document.querySelectorAll('main > section');
    sections.forEach(s => {
      if(s.id === section) s.style.display = '';
      else s.style.display = 'none';
    });
    // if showing login/register, focus first input
    if(section === 'login'){
      const el = document.getElementById('login-username'); if(el) el.focus();
    }
    if(section === 'register'){
      const el = document.getElementById('register-username'); if(el) el.focus();
    }
  }

  // wire hash change
  window.addEventListener('hashchange', () => {
    const section = location.hash.replace('#','') || 'home';
    showSection(section);
  });

  // handle login form
  const loginForm = document.getElementById('login-form');
  if(loginForm){
    loginForm.addEventListener('submit', async (e)=>{
      e.preventDefault();
      const user = document.getElementById('login-username').value.trim();
      const pass = document.getElementById('login-password').value;
      const msg = document.getElementById('login-message');
      msg.textContent = '';
      try{
        const res = await fetch('/api/auth/login', {
          method: 'POST',
          headers: {'Content-Type':'application/json'},
          body: JSON.stringify({ username: user, password: pass })
        });
        const data = await res.json();
        if(!res.ok){
          msg.textContent = data?.message || 'Login failed';
          return;
        }
        setToken(data.token);
        msg.textContent = data.message || 'Login successful';
        navigateTo('home');
      }catch(err){
        msg.textContent = 'Network error';
      }
    });
  }

  // handle register form
  const registerForm = document.getElementById('register-form');
  if(registerForm){
    registerForm.addEventListener('submit', async (e)=>{
      e.preventDefault();
      const user = document.getElementById('register-username').value.trim();
      const pass = document.getElementById('register-password').value;
      const msg = document.getElementById('register-message');
      msg.textContent = '';
      try{
        const res = await fetch('/api/auth/register', {
          method: 'POST',
          headers: {'Content-Type':'application/json'},
          body: JSON.stringify({ username: user, password: pass })
        });
        const data = await res.json();
        if(!res.ok){
          msg.textContent = data?.message || 'Registration failed';
          return;
        }
        setToken(data.token);
        msg.textContent = data.message || 'Registration successful';
        navigateTo('home');
      }catch(err){
        msg.textContent = 'Network error';
      }
    });
  }

  // initialize UI
  updateAuthUI();
  const initial = location.hash.replace('#','') || 'home';
  showSection(initial);

})();

// Fetch and display genres with their games
(function(){
  async function loadGenres(){
    try{
      const res = await fetch('/api/genres');
      if(!res.ok) throw new Error('Failed to load genres');
      const genres = await res.json();
      
      const container = document.getElementById('genres-container');
      if(!container) return;
      // determine current user role from JWT (Admin users see management controls)
      let currentUserRole = null;
      try{
        const token = localStorage.getItem('jwt');
        if(token){
          const parts = token.split('.');
          if(parts.length === 3){
            const decoded = JSON.parse(atob(parts[1]));
            currentUserRole = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
          }
        }
      }catch(e){ currentUserRole = null; }

      container.innerHTML = '';

      // Admin: Add Genre button
      if(currentUserRole === 'Admin'){
        const topBar = document.createElement('div');
        topBar.className = 'genre-topbar';
        const addGenreBtn = document.createElement('button');
        addGenreBtn.className = 'btn';
        addGenreBtn.textContent = 'Add Genre';
        addGenreBtn.addEventListener('click', ()=>{
          // open simple inline form at top
          if(topBar.querySelector('.add-genre-form')) return;
          const form = document.createElement('form');
          form.className = 'add-genre-form';
          form.innerHTML = `
            <input name="name" placeholder="Genre name" required />
            <button class="btn" type="submit">Create</button>
            <button class="btn" type="button" id="cancel-genre">Cancel</button>
            <div class="form-message"></div>
          `;
          topBar.appendChild(form);
          form.querySelector('#cancel-genre').addEventListener('click', ()=>{ form.remove(); });
          form.addEventListener('submit', async (e)=>{
            e.preventDefault();
            const msg = form.querySelector('.form-message'); msg.textContent = '';
            const name = (form.elements['name'].value||'').toString().trim();
            if(!name){ msg.textContent = 'Name required'; return; }
            try{
              const headers = {'Content-Type':'application/json'};
              const token = localStorage.getItem('jwt'); if(token) headers['Authorization'] = `Bearer ${token}`;
              const body = { name };
              const r = await fetch('/api/genres', { method: 'POST', headers, body: JSON.stringify(body) });
              if(!r.ok){ const err = await r.json().catch(()=>({})); msg.textContent = err.message || `Failed (${r.status})`; return; }
              form.remove(); loadGenres();
            }catch(err){ msg.textContent = 'Network error'; }
          });
        });
        topBar.appendChild(addGenreBtn);
        container.appendChild(topBar);
      }
      
      for(const genre of genres){
        // Fetch games for this genre
        const gamesRes = await fetch(`/api/games?genreId=${genre.id}`);
        const games = gamesRes.ok ? await gamesRes.json() : [];
        
        const genreSection = document.createElement('div');
        genreSection.className = 'genre-section';
        
        const genreTitle = document.createElement('h3');
        genreTitle.className = 'genre-title';
        genreTitle.textContent = genre.name;
        genreSection.appendChild(genreTitle);
        
        const gamesGrid = document.createElement('div');
        gamesGrid.className = 'games-grid collapsed';
        gamesGrid.dataset.genreId = genre.id;
        
        if(games.length === 0){
          const noGames = document.createElement('p');
          noGames.textContent = 'No games in this genre yet.';
          noGames.style.color = 'var(--muted)';
          gamesGrid.appendChild(noGames);
        } else {
          for(const game of games){
            const gameCard = document.createElement('div');
            gameCard.className = 'game-card';

            const ratingEl = document.createElement('div');
            ratingEl.className = 'game-card-rating';
            ratingEl.textContent = '';
            gameCard.appendChild(ratingEl);

            const gameImage = document.createElement('div');
            gameImage.className = 'game-card-image';
            // if an image URL is provided, create an <img> element; otherwise show placeholder text
            // always include a small decorative SVG so it is visible in inspector
            const svg = document.createElementNS('http://www.w3.org/2000/svg','svg');
            svg.setAttribute('class','image-icon');
            svg.setAttribute('viewBox','0 0 24 24');
            svg.innerHTML = '<rect x="2" y="2" width="20" height="20" rx="2" ry="2" fill="none" stroke="currentColor" stroke-width="1.2"></rect><circle cx="8" cy="8" r="1.8" fill="currentColor"></circle><path d="M21 21l-7-7-5 6H3" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"></path>';
            gameImage.appendChild(svg);
            if (game.imageUrl) {
              const img = document.createElement('img');
              img.className = 'game-img';
              img.src = game.imageUrl;
              img.alt = game.name || 'Game image';
              // on error, hide the broken image but keep the SVG visible as placeholder
              img.onerror = () => { img.style.display = 'none'; svg.style.display = 'block'; };
              // if image loads, we may hide the svg or leave it as a decorative element
              img.onload = () => { /* keep svg visible for inspectability */ };
              gameImage.appendChild(img);
            } else {
              // no image provided; show svg and keep a small text marker for accessibility
              const span = document.createElement('span');
              span.className = 'sr-only';
              span.textContent = '[Image]';
              gameImage.appendChild(span);
            }
            gameCard.appendChild(gameImage);

            const gameContent = document.createElement('div');
            gameContent.className = 'game-card-content';
            
            const gameTitle = document.createElement('h4');
            gameTitle.className = 'game-card-title';
            gameTitle.textContent = game.name || game.title || 'Unknown Game';
            gameContent.appendChild(gameTitle);
            
            const gameDesc = document.createElement('p');
            gameDesc.className = 'game-card-description';
            const desc = game.description || '';
            gameDesc.textContent = desc;
            gameContent.appendChild(gameDesc);
            
            gameCard.appendChild(gameContent);
            gameCard.style.cursor = 'pointer';
            gameCard.addEventListener('click', ()=>{
              // navigate to game detail by id
              if(game.id) location.hash = `#game-${game.id}`;
            });
            gamesGrid.appendChild(gameCard);

            // fetch reviews for this game to compute average rating
            (async ()=>{
              try{
                const rr = await fetch(`/api/reviews?gameId=${game.id}`);
                if(!rr.ok){
                  ratingEl.textContent = '';
                  return;
                }
                const revs = await rr.json();
                if(revs && revs.length){
                  const sum = revs.reduce((s,r)=>s + (r.rating ?? r.Rating ?? 0), 0);
                  const avg = sum / revs.length;
                  ratingEl.textContent = `${avg.toFixed(1)} ★ (${revs.length})`;
                } else {
                  ratingEl.textContent = '—';
                }
              }catch(e){
                ratingEl.textContent = '';
              }
            })();
          }
        }
        
        genreSection.appendChild(gamesGrid);

        // Admin: show Delete Genre button if this genre has no games
        if(currentUserRole === 'Admin' && (!games || games.length === 0)){
          const delGenreBtn = document.createElement('button');
          delGenreBtn.className = 'btn small danger';
          delGenreBtn.textContent = 'Delete Genre';
          delGenreBtn.style.marginLeft = '0.5rem';
          delGenreBtn.addEventListener('click', async ()=>{
            if(!confirm(`Delete genre '${genre.name}'?`)) return;
            try{
              const headers = {};
              const token = localStorage.getItem('jwt'); if(token) headers['Authorization'] = `Bearer ${token}`;
              const res = await fetch(`/api/genres/${genre.id}`, { method: 'DELETE', headers });
              if(res.ok){
                loadGenres();
                return;
              }
              // show server message if present
              const body = await res.json().catch(()=>({}));
              if(res.status === 409)
                alert(body.message || 'Cannot delete a genre that still has games.');
              else
                alert(body.message || `Delete failed (${res.status})`);
            }catch(e){ console.error(e); alert('Network error'); }
          });
          genreSection.appendChild(delGenreBtn);
        }

        // Admin: Add Game button for this genre
        if(currentUserRole === 'Admin'){
          const addGameBtn = document.createElement('button');
          addGameBtn.className = 'btn small';
          addGameBtn.textContent = 'Add Game';
          addGameBtn.addEventListener('click', ()=>{
            if(genreSection.querySelector('.add-game-form')) return;
            const form = document.createElement('form');
            form.className = 'add-game-form';
            form.innerHTML = `
              <label>Title <input name="title" required /></label>
              <label>Image URL <input name="imageUrl" /></label>
              <label>Description <textarea name="description"></textarea></label>
              <div class="form-actions">
                <button class="btn" type="submit">Create Game</button>
                <button class="btn" type="button" id="cancel-game">Cancel</button>
              </div>
              <div class="form-message"></div>
            `;
            genreSection.appendChild(form);
            form.querySelector('#cancel-game').addEventListener('click', ()=>{ form.remove(); });
            form.addEventListener('submit', async (e)=>{
              e.preventDefault();
              const msg = form.querySelector('.form-message'); msg.textContent = '';
              const title = (form.elements['title'].value||'').toString().trim();
              const imageUrl = (form.elements['imageUrl'].value||'').toString().trim();
              const description = (form.elements['description'].value||'').toString().trim();
              if(!title){ msg.textContent = 'Title required'; return; }
              try{
                const headers = {'Content-Type':'application/json'};
                const token = localStorage.getItem('jwt'); if(token) headers['Authorization'] = `Bearer ${token}`;
                const body = { title: title, description: description, imageUrl: imageUrl, genreId: genre.id };
                const r = await fetch('/api/games', { method: 'POST', headers, body: JSON.stringify(body) });
                if(!r.ok){ const err = await r.json().catch(()=>({})); msg.textContent = err.message || `Failed (${r.status})`; return; }
                form.remove(); loadGenres();
              }catch(err){ msg.textContent = 'Network error'; }
            });
          });
          genreSection.appendChild(addGameBtn);
        }
        
        // Add expand button if there are more than 4 games
        if(games.length > 4){
          const expandBtn = document.createElement('button');
          expandBtn.className = 'expand-btn';
          expandBtn.textContent = 'Show All';
          expandBtn.dataset.expanded = 'false';
          expandBtn.onclick = function(){
            const isExpanded = expandBtn.dataset.expanded === 'true';
            if(isExpanded){
              gamesGrid.classList.add('collapsed');
              expandBtn.textContent = 'Show All';
              expandBtn.dataset.expanded = 'false';
            } else {
              gamesGrid.classList.remove('collapsed');
              expandBtn.textContent = 'Show Less';
              expandBtn.dataset.expanded = 'true';
            }
          };
          genreSection.appendChild(expandBtn);
        }
        
        container.appendChild(genreSection);
      }
    }catch(err){
      console.error('Error loading genres:', err);
      const container = document.getElementById('genres-container');
      if(container){
        container.innerHTML = '<p style="color:var(--muted)">Failed to load genres. Please try again later.</p>';
      }
    }
  }
  
  // Load genres when page loads or when genres section is shown
  window.addEventListener('hashchange', () => {
    const section = location.hash.replace('#','');
    if(section === 'genres'){
      loadGenres();
    }
  });
  
  // Also load on initial load if starting at genres
  const initial = location.hash.replace('#','') || 'home';
  if(initial === 'genres'){
    loadGenres();
  }
})();

// Game detail loader - handles hashes like #game-{id}
(function(){
  function renderStars(rating){
    const r = Math.max(0, Math.min(5, Math.round(rating||0)));
    let out = '';
    for(let i=1;i<=5;i++){
      out += `<span class="star ${i<=r? 'filled':''}">★</span>`;
    }
    return out;
  }

  async function loadGameDetail(id){
    const detailSection = document.getElementById('game-detail');
    const titleEl = document.getElementById('game-detail-title');
    const imageEl = document.getElementById('game-detail-image');
    const descEl = document.getElementById('game-detail-description');
    const reviewsList = document.getElementById('reviews-list');

    if(!detailSection) return;
    // clear
    titleEl.textContent = 'Loading...';
    imageEl.innerHTML = '[Image]';
    descEl.textContent = '';
    reviewsList.innerHTML = '';

    // Determine current user ID and role from JWT (if present)
    function getCurrentUserId(){
      try{
        const token = localStorage.getItem('jwt');
        if(!token) return null;
        const parts = token.split('.');
        if(parts.length !== 3) return null;
        const decoded = JSON.parse(atob(parts[1]));
        const guidString = decoded.sub || decoded.nameid || decoded.nameidentifier ||
               decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
               decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier'] || null;
        if(guidString){
          return Math.abs(guidString.split('').reduce((hash,c)=>((hash<<5)-hash)+c.charCodeAt(0)|0, 0));
        }
        return null;
      }catch(e){ return null; }
    }

    function getCurrentUserRole(){
      try{
        const token = localStorage.getItem('jwt');
        if(!token) return null;
        const parts = token.split('.');
        if(parts.length !== 3) return null;
        const decoded = JSON.parse(atob(parts[1]));
        return decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.rc || null;
      }catch(e){ return null; }
    }

    const currentUserId = getCurrentUserId();
    const currentUserRole = getCurrentUserRole();

    try{
      // Try to get single game
      let gameRes = await fetch(`/api/games/${id}`);
      let game = null;
      if(gameRes.ok){
        game = await gameRes.json();
      } else {
        // fallback: fetch all games and find
        const allRes = await fetch('/api/games');
        if(allRes.ok){
          const all = await allRes.json();
          game = all.find(g => String(g.id) === String(id));
        }
      }

      if(!game){
        titleEl.textContent = 'Game not found';
        return;
      }

      titleEl.textContent = game.name || game.title || 'Untitled Game';
      descEl.textContent = game.description || '';

      // Admin-only: inline Edit Description button next to the description
      try{
        if(currentUserRole === 'Admin'){
          // avoid adding multiple buttons
          if(!descEl.nextSibling || !descEl.parentNode.querySelector('.edit-desc-btn')){
            const editBtn = document.createElement('button');
            editBtn.className = 'btn small edit-desc-btn';
            editBtn.textContent = 'Edit Description';
            editBtn.style.marginLeft = '0.5rem';
            editBtn.addEventListener('click', ()=>{
              // if form already open, do nothing
              if(document.getElementById('desc-edit-form')) return;
              const form = document.createElement('form');
              form.id = 'desc-edit-form';
              form.className = 'desc-edit-form';
              form.innerHTML = `
                <textarea name="description" rows="6" style="width:100%"></textarea>
                <div class="form-actions" style="margin-top:.5rem">
                  <button class="btn" type="submit">Save</button>
                  <button class="btn" type="button" id="cancel-desc-edit">Cancel</button>
                </div>
                <div class="form-message"></div>
              `;
              // prefill
              form.elements['description'].value = game.description || '';
              descEl.style.display = 'none';
              descEl.parentNode.insertBefore(form, descEl.nextSibling);
              form.querySelector('#cancel-desc-edit').addEventListener('click', ()=>{
                form.remove();
                descEl.style.display = '';
              });
              form.addEventListener('submit', async (e)=>{
                e.preventDefault();
                const msg = form.querySelector('.form-message'); msg.textContent = '';
                const newDesc = (form.elements['description'].value||'').toString().trim();
                try{
                  const headers = {'Content-Type':'application/json'};
                  const token = localStorage.getItem('jwt'); if(token) headers['Authorization'] = `Bearer ${token}`;
                  const body = { id: game.id || game.Id, title: game.title || game.Title || '', description: newDesc, imageUrl: game.imageUrl || game.ImageUrl || '', genreId: game.genreId || game.GenreId };
                  const res = await fetch('/api/games', { method: 'PUT', headers, body: JSON.stringify(body) });
                  if(!res.ok){ const err = await res.json().catch(()=>({})); msg.textContent = err.message || `Failed to save (${res.status})`; return; }
                  // reload detail
                  form.remove();
                  descEl.style.display = '';
                  loadGameDetail(id);
                }catch(err){ console.error(err); msg.textContent = 'Network error'; }
              });
            });
            descEl.parentNode.insertBefore(editBtn, descEl.nextSibling);
          }
        }
      }catch(e){ /* ignore */ }

      // image
      imageEl.innerHTML = '';
      // include decorative SVG so inspector shows an svg.image-icon section
      const detailSvg = document.createElementNS('http://www.w3.org/2000/svg','svg');
      detailSvg.setAttribute('class','image-icon');
      detailSvg.setAttribute('viewBox','0 0 24 24');
      detailSvg.innerHTML = '<rect x="2" y="2" width="20" height="20" rx="2" ry="2" fill="none" stroke="currentColor" stroke-width="1.2"></rect><circle cx="8" cy="8" r="1.8" fill="currentColor"></circle><path d="M21 21l-7-7-5 6H3" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"></path>';
      imageEl.appendChild(detailSvg);
      if(game.imageUrl){
        const img = document.createElement('img');
        img.src = game.imageUrl;
        img.alt = game.name || 'Game image';
        img.onerror = ()=>{ img.style.display = 'none'; detailSvg.style.display = 'block'; };
        img.onload = ()=>{ /* leave SVG for inspectability */ };
        imageEl.appendChild(img);
      } else {
        const span = document.createElement('span'); span.className = 'sr-only'; span.textContent = '[Image]'; imageEl.appendChild(span);
      }

      // load reviews
      const revRes = await fetch(`/api/reviews?gameId=${id}`);
      let reviews = [];
      if(revRes.ok){
        reviews = await revRes.json();
      }

      // (currentUserId and currentUserRole are initialized earlier)

      // Find user's review if logged in
      let userReview = null;
      if(currentUserId && reviews && reviews.length){
        userReview = reviews.find(r => r.userId === currentUserId || r.UserId === currentUserId);
      }

      // compute and display average in detail view
      const avgEl = document.getElementById('game-average');
      if(avgEl){
        if(!reviews || reviews.length === 0){
          avgEl.textContent = 'No ratings';
        } else {
          const sum = reviews.reduce((s,r)=>s + (r.rating ?? r.Rating ?? 0), 0);
          const avg = sum / reviews.length;
          avgEl.innerHTML = `${renderStars(avg)} <span style="margin-left:.5rem;font-weight:600">${avg.toFixed(1)} (${reviews.length})</span>`;
        }
      }

      // render review form area (if logged in and hasn't reviewed yet)
      const formArea = document.getElementById('review-form-area');
      formArea.innerHTML = '';
      if(currentUserId && !userReview){
        const f = document.createElement('form');
        f.className = 'review-form';
        f.innerHTML = `
          <h4>Leave a Review</h4>
          <label>Rating
            <select name="rating" class="rating-select">
              <option value="1">1 - Poor</option>
              <option value="2">2 - Fair</option>
              <option value="3">3 - Good</option>
              <option value="4">4 - Very Good</option>
              <option value="5" selected>5 - Excellent</option>
            </select>
          </label>
          <label>Comment
            <textarea name="comment" placeholder="Share your thoughts about this game..." required></textarea>
          </label>
          <div class="form-actions">
            <button class="btn" type="submit">Submit Review</button>
          </div>
          <div class="form-message" id="review-form-message"></div>
        `;
        formArea.appendChild(f);

        f.addEventListener('submit', async (e)=>{
          e.preventDefault();
          const formMsg = document.getElementById('review-form-message');
          formMsg.textContent = '';
          const formData = new FormData(f);
          const rating = parseInt(formData.get('rating')) || 5;
          const comment = (formData.get('comment')||'').toString().trim();
          if(!comment){ formMsg.textContent = 'Please enter a comment.'; return; }

          // Server sets UserId from JWT; client sends only rating/comment/gameId
          const token = localStorage.getItem('jwt');
          const payload = {
            rating: rating,
            comment: comment,
            gameId: parseInt(id)
          };

          try{
            const headers = {'Content-Type':'application/json'};
            if(token) headers['Authorization'] = `Bearer ${token}`;
            const postRes = await fetch('/api/reviews', { 
              method: 'POST', 
              headers, 
              body: JSON.stringify(payload) 
            });
            if(!postRes.ok){
              const err = await postRes.json().catch(()=>({}));
              const errorText = err.message || err.title || 'Failed to post review.';
              formMsg.textContent = `Error (${postRes.status}): ${errorText}`;
              return;
            }
            // success - reload detail
            formMsg.textContent = 'Review posted!';
            setTimeout(()=>{ loadGameDetail(id); }, 500);
          }catch(err){
            formMsg.textContent = 'Network error';
            console.error(err);
          }
        });
      }

      // helper to open inline edit UI for a review
      function openEditForm(containerItem, review){
        // replace content area with an edit form
        const body = containerItem.querySelector('.review-body');
        const meta = containerItem.querySelector('.review-meta');
        if(!body || !meta) return;
        // hide existing body
        body.style.display = 'none';
        // avoid opening multiple forms
        if(containerItem.querySelector('.edit-form')) return;

        const form = document.createElement('form');
        form.className = 'edit-form';
        form.innerHTML = `
          <label>Rating
            <select name="rating" class="rating-select">
              <option value="1">1</option>
              <option value="2">2</option>
              <option value="3">3</option>
              <option value="4">4</option>
              <option value="5">5</option>
            </select>
          </label>
          <label>Comment
            <textarea name="comment"></textarea>
          </label>
          <div class="form-actions">
            <button class="btn" type="submit">Save</button>
            <button class="btn" type="button" id="cancel-edit">Cancel</button>
          </div>
          <div class="form-message"></div>
        `;
        // prefill
        const sel = form.querySelector('select[name=rating]');
        const ta = form.querySelector('textarea[name=comment]');
        if(sel) sel.value = String(review.rating || review.Rating || 5);
        if(ta) ta.value = review.comment || review.Comment || '';

        meta.parentNode.insertBefore(form, body.nextSibling);

        form.querySelector('#cancel-edit').addEventListener('click', ()=>{
          form.remove();
          body.style.display = '';
        });

        form.addEventListener('submit', async (e)=>{
          e.preventDefault();
          const msg = form.querySelector('.form-message');
          msg.textContent = '';
          const formData = new FormData(form);
          const rating = parseInt(formData.get('rating')) || 5;
          const comment = (formData.get('comment')||'').toString().trim();
          if(!comment){ msg.textContent = 'Please enter a comment.'; return; }

          // prepare payload
          const payload = {
            id: review.id || review.Id || review.ID,
            rating: rating,
            comment: comment,
            gameId: review.gameId || review.GameId || parseInt(id),
            userId: review.userId || review.UserId
          };
          try{
            const headers = {'Content-Type':'application/json'};
            const token = localStorage.getItem('jwt');
            if(token) headers['Authorization'] = `Bearer ${token}`;
            const res = await fetch('/api/reviews', { method: 'PUT', headers, body: JSON.stringify(payload) });
            if(!res.ok){
              const err = await res.json().catch(()=>({}));
              msg.textContent = err.message || `Failed to save (${res.status})`;
              return;
            }
            loadGameDetail(id);
          }catch(e){ console.error(e); msg.textContent = 'Network error'; }
        });
      }

      // render reviews (user's review first and highlighted, then others)
      reviewsList.innerHTML = '';
      if(userReview){
        // Show user's review first, highlighted
        const item = document.createElement('div');
        item.className = 'review-item highlight';
        const meta = document.createElement('div');
        meta.className = 'review-meta';
        const author = document.createElement('div');
        author.className = 'review-author';
        author.textContent = 'Your Review';
        const stars = document.createElement('div');
        stars.className = 'review-stars';
        stars.innerHTML = renderStars(userReview.rating || userReview.Rating || 0);
        // controls for the highlighted (current user's) review
        const controls = document.createElement('div');
        controls.className = 'review-controls';

        // determine ownership and role (for highlighted it's the owner's review)
        const isOwnerFlag = true;

        // Delete button (owner or admin)
        if(currentUserRole === 'Admin' || isOwnerFlag){
          const del = document.createElement('button');
          del.className = 'btn small danger';
          del.textContent = 'Delete';
          del.addEventListener('click', async (ev)=>{
            ev.stopPropagation();
            if(!confirm('Delete this review?')) return;
            try{
              const headers = {};
              const token = localStorage.getItem('jwt');
              if(token) headers['Authorization'] = `Bearer ${token}`;
              const rid = userReview.id || userReview.Id;
              const res = await fetch(`/api/reviews/${rid}`, { method: 'DELETE', headers });
              if(!res.ok){
                alert(`Delete failed (${res.status})`);
                return;
              }
              loadGameDetail(id);
            }catch(e){ console.error(e); alert('Network error'); }
          });
          controls.appendChild(del);
        }

        // Edit button (owner only)
        if(isOwnerFlag){
          const edit = document.createElement('button');
          edit.className = 'btn small';
          edit.textContent = 'Edit';
          edit.addEventListener('click', (ev)=>{
            ev.stopPropagation();
            openEditForm(item, userReview);
          });
          controls.appendChild(edit);
        }

        meta.appendChild(author);
        meta.appendChild(stars);
        meta.appendChild(controls);

        const body = document.createElement('div');
        body.className = 'review-body';
        body.textContent = userReview.comment || userReview.Comment || '';
        item.appendChild(meta);
        item.appendChild(body);
        reviewsList.appendChild(item);
      }

      // render other reviews (excluding user's if present)
      if(!reviews || reviews.length === 0){
        if(!userReview) reviewsList.innerHTML = '<p class="muted">No reviews yet.</p>';
      } else {
        for(const r of reviews){
          // Skip user's review (already rendered above)
          if(userReview && String(r.userId || r.UserId) === String(currentUserId)) continue;
          
          const item = document.createElement('div');
          item.className = 'review-item';
          const meta = document.createElement('div');
          meta.className = 'review-meta';
          const author = document.createElement('div');
          author.className = 'review-author';
          author.textContent = r.username || r.userName || r.userId || 'Anonymous';
          const date = document.createElement('div');
          date.className = 'review-date';
          date.textContent = r.createdAt ? new Date(r.createdAt).toLocaleDateString() : '';
          const stars = document.createElement('div');
          stars.className = 'review-stars';
          stars.innerHTML = renderStars(r.rating || r.Rating || r.stars || 0);

          // controls container (appears at right)
          const controls = document.createElement('div');
          controls.className = 'review-controls';

          // Delete button: visible to admins for any review, and to owners for their own
          const isOwnerFlag = r.isOwner || r.IsOwner || (currentUserId && (r.userId === currentUserId || r.UserId === currentUserId));
          if(currentUserRole === 'Admin' || isOwnerFlag){
            const del = document.createElement('button');
            del.className = 'btn small danger';
            del.textContent = 'Delete';
            del.addEventListener('click', async (ev)=>{
              ev.stopPropagation();
              if(!confirm('Delete this review?')) return;
              try{
                const headers = {};
                const token = localStorage.getItem('jwt');
                if(token) headers['Authorization'] = `Bearer ${token}`;
                const res = await fetch(`/api/reviews/${r.id}`, { method: 'DELETE', headers });
                if(!res.ok){
                  alert(`Delete failed (${res.status})`);
                  return;
                }
                loadGameDetail(id);
              }catch(e){ console.error(e); alert('Network error'); }
            });
            controls.appendChild(del);
          }

          // Edit button: visible to the review owner (user who posted it)
          if(isOwnerFlag){
            const edit = document.createElement('button');
            edit.className = 'btn small';
            edit.textContent = 'Edit';
            edit.addEventListener('click', (ev)=>{
              ev.stopPropagation();
              openEditForm(item, r);
            });
            controls.appendChild(edit);
          }

          meta.appendChild(author);
          meta.appendChild(date);
          meta.appendChild(stars);
          meta.appendChild(controls);

          const body = document.createElement('div');
          body.className = 'review-body';
          body.textContent = r.comment || r.Comment || r.text || '';

          item.appendChild(meta);
          item.appendChild(body);
          reviewsList.appendChild(item);
        }
      }

    }catch(err){
      console.error('Failed to load game detail', err);
      titleEl.textContent = 'Error loading game';
    }

    // ADMIN: game-level controls (Edit description, Delete game)
    // We add buttons to the page header (near title) when current user is Admin
    const headerControls = document.getElementById('game-detail-controls');
    if(headerControls) headerControls.innerHTML = '';
    if(currentUserRole === 'Admin'){
      // Delete button
      if(headerControls){
        // Header-level 'Edit Description' removed — inline editor next to description is sufficient.

        const del = document.createElement('button');
        del.className = 'btn danger';
        del.textContent = 'Delete Game';
        del.addEventListener('click', async ()=>{
          if(!confirm('Delete this game? This will remove all reviews.')) return;
          try{
            const headers = {};
            const token = localStorage.getItem('jwt'); if(token) headers['Authorization'] = `Bearer ${token}`;
            const res = await fetch(`/api/games/${id}`, { method: 'DELETE', headers });
            if(!res.ok){ const err = await res.json().catch(()=>({})); alert(err.message || `Delete failed (${res.status})`); return; }
            // after delete go back to genres
            location.hash = '#genres';
          }catch(e){ console.error(e); alert('Network error'); }
        });
        headerControls.appendChild(del);

        // Note: 'Edit Game' inline form removed to keep header controls minimal.
      }
    }

  }
  window.addEventListener('hashchange', ()=>{
    const h = location.hash.replace('#','');
    if(h && h.startsWith('game-')){
      const id = h.replace('game-','');
      // show game-detail section
      const sections = document.querySelectorAll('main > section');
      sections.forEach(s => s.style.display = s.id === 'game-detail' ? '' : 'none');
      loadGameDetail(id);
    }
  });

  // if initial hash is a game
  const ih = location.hash.replace('#','');
  if(ih && ih.startsWith('game-')){
    const id = ih.replace('game-','');
    const sections = document.querySelectorAll('main > section');
    sections.forEach(s => s.style.display = s.id === 'game-detail' ? '' : 'none');
    loadGameDetail(id);
  }

})();
