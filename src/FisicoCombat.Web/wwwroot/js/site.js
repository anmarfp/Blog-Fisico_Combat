// Scripts compartilhados do blog Físico Combat.

// Compartilhar (RF11): "Copiar link" e o compartilhamento nativo só aparecem
// quando o navegador oferece o recurso; sem JavaScript os links continuam funcionando.
document.querySelectorAll('[data-compartilhar]').forEach(function (bloco) {
  var url = bloco.dataset.url;
  var titulo = bloco.dataset.titulo;
  var status = bloco.querySelector('[data-status]');
  var copiar = bloco.querySelector('[data-acao="copiar"]');
  var nativo = bloco.querySelector('[data-acao="nativo"]');

  if (navigator.clipboard && window.isSecureContext) {
    copiar.hidden = false;
    copiar.addEventListener('click', function () {
      navigator.clipboard.writeText(url).then(function () {
        status.textContent = 'Link copiado!';
      }, function () {
        status.textContent = 'Não foi possível copiar o link.';
      });
    });
  }

  if (navigator.share) {
    nativo.hidden = false;
    nativo.addEventListener('click', function () {
      navigator.share({ title: titulo, url: url }).catch(function () {
        // O usuário cancelou o compartilhamento; nada a fazer.
      });
    });
  }
});
